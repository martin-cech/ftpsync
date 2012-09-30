using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Net;
using System.Text;
using FtpSync.Helpers;
using FtpSync.Utils;

namespace FtpSync.Components
{
	public class FtpClient
	{
		private readonly Configuration _configuration;
		private readonly TrackedFiles _trackedFiles;
		private readonly NetworkCredential _credentials;
		private readonly ConflictResolver _conflictResolver;

		public FtpClient(string trackedFilesPath, Configuration configuration)
		{
			_configuration = configuration;
			_trackedFiles = TrackedFiles.TryLoad(trackedFilesPath);
			_credentials = new NetworkCredential(configuration.Username, configuration.Password);
			_conflictResolver = new ConflictResolver(_configuration);
		}

		public void MakeDirectory(string path)
		{
			Execute(WebRequestMethods.Ftp.MakeDirectory, path);
		}

		public void DeleteEmptyDirectory(string path)
		{
			Execute(WebRequestMethods.Ftp.RemoveDirectory, path);
		}

		public void DeleteDirectory(FtpDirInfo ftpDir, bool deleteRoot = true)
		{
			if (!ftpDir.IsEmpty())
			{
				foreach (var file in ftpDir.Files)
				{
					DeleteFile(file.FileName);
				}

				foreach (var dir in ftpDir.Directories)
				{
					DeleteDirectory(GetDirectoryContent(dir.FullPath, false));
				}
			}

			if (deleteRoot) DeleteEmptyDirectory(ftpDir.FullPath);
		}

		public void DeleteFile(string path)
		{
			Execute(WebRequestMethods.Ftp.DeleteFile, path);
		}

		public long UploadFile(string filename, string path)
		{
			return Execute(WebRequestMethods.Ftp.UploadFile, path,
			               req =>
			               	{
			               		//filename.Dump();
			               		using (var fileStream = new FileStream(filename, FileMode.Open))
			               		using (var requestStream = req.GetRequestStream())
			               		{
			               			return fileStream.WriteAll(requestStream);
			               		}
			               	});

		}

		public string GetWorkingDirectory(string path)
		{
			return ExecuteStreamString(WebRequestMethods.Ftp.PrintWorkingDirectory, path);
		}

		public byte[] DownloadFile(string path)
		{
			return ExecuteStream(WebRequestMethods.Ftp.DownloadFile, path);
		}

		public FtpDirInfo GetDirectoryContent(string ftpPath, bool recursive = false)
		{
			var filelist = GetFileList(ftpPath).OrderByDescending(s => s.Length).ToArray();
			var details = GetFileListDetail(ftpPath).ToList();

			var result = new FtpDirInfo {FullPath = ftpPath};

			foreach (var filename in filelist)
			{
				// HACK here... but shh, that works quite smooth ow
				var fullInfo = details.FirstOrDefault(dir => dir.EndsWith(" " + filename));

				if (fullInfo == null)
				{
					Log.Warning("Couldn't verify file {0} on server.".Expand(filename));
					continue;
				}

				// remove it from list - this must be here (because of conflict with suffix errors like a.php and aa.php)
				details.Remove(fullInfo);

				if (fullInfo.StartsWith("d"))
				{
					if (recursive)
					{
						result.AddDirectory(GetDirectoryContent(ftpPath.CombineFtp(filename)), fullInfo);
					}
					else
					{
						result.AddDirectory(filename, fullInfo);
					}
				}
				else
				{
					result.AddFile(filename, fullInfo);
				}
			}

			return result;
		}

		public IEnumerable<string> GetFileList(string path)
		{
			return ExecuteStreamString(WebRequestMethods.Ftp.ListDirectory, path)
				.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries)
				.Select(Path.GetFileName);
		}

		public IEnumerable<string> GetFileListDetail(string path)
		{
			return ExecuteStreamString(WebRequestMethods.Ftp.ListDirectoryDetails, path)
				.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
			//.Select(s => s.Trim('"', '\'', ' '));
		}

		private void Execute(string method, string path)
		{
			Execute(method, path, req => req.GetResponse());
		}

		private T Execute<T>(string method, string path, Func<FtpWebRequest, T> onExecute)
		{
			var req = (FtpWebRequest) WebRequest.Create(path);
			req.Method = method;
			req.Credentials = _credentials;
			req.KeepAlive = true;

			return onExecute(req);
		}

		private long ExecuteInt(string method, string path)
		{
			return Execute(method, path, req => req.GetResponse().ContentLength);
		}

		private string ExecuteStreamString(string method, string path)
		{
			return Encoding.Default.GetString(ExecuteStream(method, path));
		}

		private byte[] ExecuteStream(string method, string path)
		{
			return Execute(method, path, req => req.GetResponse().GetResponseStream().ReadAll());
		}

		public void Synchronize()
		{
			try
			{
				Sync(_configuration.LocalFolder, _configuration.ServerRoot, _configuration);
			}
			catch(Exception ex)
			{
				Handle(ex);
			}

			// always save changes - if some files were uploaded, we need to know!
			_trackedFiles.Save();
		}


		private void Sync(string localDirectory, string ftpPath, Configuration config)
		{
			var localFiles = Directory.GetFiles(localDirectory);

			var ftpDirectory = GetDirectoryContent(ftpPath, false);

			var unprocessedFiles = new HashSet<FtpFileInfo>(ftpDirectory.Files);
			var localFileInfos = new HashSet<SyncFileInfo>();

			_trackedFiles.TrackDirectory(localDirectory, ftpDirectory.FullPath);

			// check all files in current directory
			foreach (var localFilePath in localFiles)
			{
				var filename = Path.GetFileName(localFilePath);
				var fullLocalPath = localDirectory.CombinePath(filename);
				var syncFileInfo = new SyncFileInfo
				                   	{
				                   		LocalPath = fullLocalPath,
				                   		LocalDetail = GetFileFullInfo(localFilePath),
				                   		UpdateFtpDetail = true
				                   	};

				bool performUpload = false;
				var ftpInfo = ftpDirectory.Files.FirstOrDefault(i => i.FileName == ftpDirectory.FullPath.CombineFtp(filename));

				try
				{
					localFileInfos.Add(syncFileInfo);

					var oldSyncInfo = _trackedFiles.Files.ByLocalPath(fullLocalPath); //.FirstOrDefault(i => i.LocalPath == fullLocalPath);
					
					if (ftpInfo != null) unprocessedFiles.Remove(ftpInfo);

					if (oldSyncInfo == null || string.IsNullOrEmpty(oldSyncInfo.FtpDetail))
					{
						// file is untracked => new on client
						if (ftpInfo == null)
						{
							// file is not on server -> simple upload
							performUpload = true;
						}
						else
						{
							// file is already on server but not localy treated, we don't know, if the server and client file are the same.
							// Idea: we could download the file and compare, then we'd know if there is conflict or not,
							// but if the files differ, we would still have to decide wheather overwrite or skip. This is easier.

							if (_conflictResolver.OverwriteUntrackedFile(filename))
							{
								Log.Warning("File {0} is already on server, overwritten.".Expand(filename));
								performUpload = true;
							}
							else
							{
								Log.Warning("File {0} is already on server, skipped.".Expand(filename));
								syncFileInfo.UpdateFtpDetail = false;
							}
						}
					}
					else
					{
						// tracked file

						if (ftpInfo == null)
						{
							// not on server -> new -> upload
							performUpload = true;
						}
						else
						{
							// we have current version from server
							if (oldSyncInfo.FtpDetail == ftpInfo.FtpDetail)
							{
								// file has changed locally or we upload all files -> upload
								if ((oldSyncInfo.LocalDetail != GetFileFullInfo(localFilePath)) || !config.UploadChangesOnly)
								{
									performUpload = true;
								}
								else
								{
									Log.Verbose("File not changed ({0})".Expand(filename));
								}
							}
							else
							{
								// server has different version than we have -> server change -> conflict
								if (_conflictResolver.OverwriteServerModified(filename))
								{
									Log.Warning("File {0} modified on server, overwritten.".Expand(filename));
									performUpload = true;
								}
								else
								{
									Log.Warning("File {0} modified on server, skipped.".Expand(filename));
									syncFileInfo.UpdateFtpDetail = false;
								}
							}
						}
					}

					if (performUpload)
					{
						Try(() =>
						    UploadFile(localFilePath, ftpDirectory.FullPath.CombineFtp(filename)),
						    "Uploading file {0}... ".Expand(filename),
						    len => "{0} bytes uploaded".Expand(len));

						//Log.Info("File {0} uploaded ({1} bytes)".Expand(filename, len));
					}
				}
				catch (Exception e)
				{
					Handle(e);
					syncFileInfo.UpdateFtpDetail = false;
					//if (!conflictedFiles.Contains(ftpInfo)) conflictedFiles.Add(ftpInfo);
				}
			}

			// delete files
			foreach (var fileInfo in unprocessedFiles)
			{
				var isTracked = _trackedFiles.Files.ByFtpDetail(fileInfo.FtpDetail) != null;

				// TODO: untracked files might be also downloaded and keep tracked!

				if (_conflictResolver.DeleteFile(fileInfo.FileName, isTracked))
				{
					Try(() => DeleteFile(fileInfo.FileName), "Deleting file {0}...".Expand(fileInfo.FileName));
				}
			}

			// recursively process directories on client
			var localDirs = Directory.GetDirectories(localDirectory);

			var ftpDirsToDelete = ftpDirectory.Directories.ToList();

			// check directories
			foreach (var localDir in localDirs)
			{
				try
				{
					var dirname = new DirectoryInfo(localDir).Name;
					var ftpSubdirFullPath = ftpDirectory.FullPath.CombineFtp(dirname);
					var subdirInfo = ftpDirectory.Directories.FirstOrDefault(d => d.FullPath == ftpSubdirFullPath);

					bool dirExists = true;

					if (subdirInfo == null)
					{
						// directory is not on server => create

						Try(() => MakeDirectory(
							path: ftpSubdirFullPath),
						    initialMessage: "Creating directory {0}...".Expand(dirname),
						    onError: () =>
						    	{
						    		dirExists = false;
						    	});
					}
					else
					{
						ftpDirsToDelete.Remove(subdirInfo);
					}

					if (dirExists)
					{
						Sync(localDirectory.CombinePath(dirname), ftpSubdirFullPath, config);
					}
					else
					{
						Log.Error("Directory {0} was not synchronized.".Expand(dirname));
					}
				}
				catch (Exception ex)
				{
					Handle(ex);
				}
			}

			// delete ftp directories which have been deleted on client (optional)
			{
				ftpDirsToDelete.ForEach(
					ftpDirToDelete =>
						{
							bool isTracked = _trackedFiles.IsDirectoryTracked(ftpDirToDelete.FullPath);

							if (_conflictResolver.DeleteDirectory(ftpDirToDelete.FullPath, isTracked))
							{
								Try(
									() => DeleteDirectory(GetDirectoryContent(ftpDirToDelete.FullPath)),
									"Deleting directory {0}...".Expand(ftpDirToDelete.FullPath)
									);
							}
						});
			}

			// update ftpinfo on local settings to remember the current state for each file (not directories)
			var ftpInfos = GetDirectoryContent(ftpDirectory.FullPath, false);

			// update local files state - we already have everything except ftpinfo in syncInfos
			foreach (var localInfo in localFileInfos)
			{
				var filename = Path.GetFileName(localInfo.LocalPath);
				var fullFtpPath = ftpDirectory.FullPath.CombineFtp(filename);

				if (localInfo.UpdateFtpDetail)
				{
					// update ftpinfo so that we keep current state for next update
					localInfo.FtpDetail = ftpInfos.Files.Where(f => f.FileName == fullFtpPath).Select(f => f.FtpDetail).FirstOrDefault();
				}
				else
				{
					// for conflicted files use old ftpinfo, so that it's used next time again
					var fileInfo = _trackedFiles.Files.ByLocalPath(localInfo.LocalPath);
					if (fileInfo != null)
					{
						localInfo.FtpDetail = fileInfo.FtpDetail;
					}
					else
					{
						Log.Warning("Could not update information for file {0}".Expand(localInfo.LocalPath));
					}
				}

				_trackedFiles.TrackFile(localInfo);
			}
		}

		private void Try(Action action, string initialMessage, string successMessage = " success", Action onSuccess = null, Action onError = null)
		{
			try
			{
				Log.Info(initialMessage, LogOptions.NoNewLine);
				action();
				Log.Info(successMessage);
				if (onSuccess != null) onSuccess();
			}
			catch(Exception e)
			{
				if (onError != null) onError();
				if (!Handle(e)) throw;
			}
		}

		private void Try<T>(Func<T> func, string initialMessage, Func<T, string> successMessage = null, Action<T> onSuccess = null, Action onError = null)
		{
			try
			{
				Log.Info(initialMessage, LogOptions.NoNewLine);
				var result = func();
				Log.Info(successMessage != null ? successMessage(result) : "");
				if (onSuccess != null) onSuccess(result);
			}
			catch(Exception e)
			{
				if (onError != null) onError();
				if (!Handle(e)) throw;
			}
		}

		// TODO: out of this class
		public static bool Handle(Exception e)
		{
			Log.Error(e.Message);
			Log.Error(e.StackTrace);
#if DEBUG
			return false;
#else
			return true;
#endif
		}

		private static string GetFileFullInfo(string fullfilename)
		{
			var fileinfo = new FileInfo(fullfilename);
			return "{0} {1} {2}".Expand(fileinfo.CreationTime, fileinfo.LastWriteTime, fileinfo.Length);
		}
	}
}