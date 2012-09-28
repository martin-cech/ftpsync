using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Net;
using System.Text;

namespace FtpSync
{
	public class FtpClient
	{
		private readonly Configuration _configuration;
		private readonly NetworkCredential _credentials;
		private readonly string _configPath;
		private readonly IConflictResolver _conflictResolver;

		public FtpClient(string configPath)
		{
			_configPath = configPath;
			_configuration = Configuration.LoadFromFile(_configPath);
			_credentials = new NetworkCredential(_configuration.Username, _configuration.Password);

			_conflictResolver = _configuration.AskOnConflict ? (IConflictResolver) new CuriousConflictResolver() : new DefaultConflictResolver(_configuration);
		}

		public void MakeDirectory(string path)
		{
			Execute(WebRequestMethods.Ftp.MakeDirectory, path);
		}

		public void DeleteEmptyDirectory(string path)
		{
			Execute(WebRequestMethods.Ftp.RemoveDirectory, path);
		}

		public void DeleteDirectory(string path)
		{
			var ftpDir = GetDirectoryContent(path, false);
			DeleteDirectory(ftpDir, "", true);
		}

		public void DeleteDirectory(FtpDirInfo dirInfo, string root, bool deleteRoot = true)
		{
			if (!dirInfo.IsEmpty())
			{
				foreach (var file in dirInfo.Files)
				{
					DeleteFile(file.FileName);
				}

				foreach (var dir in dirInfo.Directories)
				{
					DeleteDirectory(dir, root.CombineFtp(dirInfo.DirName));
				}
			}

			if (deleteRoot) DeleteEmptyDirectory(root.CombineFtp(dirInfo.DirName));
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

		public FtpDirInfo GetDirectoryContent(string ftpPath, bool recursive = true)
		{
			var filelist = GetFileList(ftpPath).OrderByDescending(s => s.Length).ToArray();
			var details = GetFileListDetail(ftpPath).ToList();

			var result = new FtpDirInfo {DirName = ftpPath};

			foreach (var filename in filelist)
			{
				// THIS IS SHIT! if there are files with names like "a.php" and "a a.php", this fails. Oh my god...
				// I need much smarter parser for detailed filelist, damned. Now we just go from longest file 
				var fullInfo = details.FirstOrDefault(dir => dir.EndsWith(" " + filename));

				if (fullInfo == null)
				{
					Log.Warning("Couldn't verify file {0} on server.".Expand(filename));
					continue;
				}

				// remove it -> no conflicts with shitty files
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
				Sync(_configuration.LocalFolder, _configuration.ServerRoot, "", _configuration);
			}
			catch(Exception ex)
			{
				Handle(ex);
			}

			// always save changes - if some files were uploaded, we need to know!
			_configuration.Save();
		}


		private void Sync(string localDirectory, string ftpDir, string parentDir, Configuration config)
		{
			var files = Directory.GetFiles(localDirectory);

			var ftpDirectory = GetDirectoryContent(parentDir.CombineFtp(ftpDir), false);
			var filesToDelete = ftpDirectory.Files.ToList();

			var localFileInfos = new Dictionary<string, SyncFileInfo>();

			// check all files in current directory
			foreach (var fullfilename in files)
			{
				var filename = Path.GetFileName(fullfilename);
				var fullLocalPath = localDirectory.CombinePath(filename);
				var syncFileInfo = new SyncFileInfo
				                   	{
				                   		LocalPath = fullLocalPath,
				                   		LocalDetail = GetFileFullInfo(fullfilename),
				                   		UpdateFtpDetail = true
				                   	};

				bool performUpload = false;
				var ftpInfo = ftpDirectory.Files.FirstOrDefault(i => i.FileName == ftpDirectory.DirName.CombineFtp(filename));

				try
				{
					localFileInfos.Add(filename, syncFileInfo);

					var oldSyncInfo = config.SyncInfos.FirstOrDefault(i => i.LocalPath == fullLocalPath);


					if (ftpInfo != null) filesToDelete.Remove(ftpInfo);

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

							if (_conflictResolver.OverwriteFileWhenNoLocalInfo(filename))
							{
								Log.Warning("File {0} is already on server, overwritten (change IgnoreInitialServerChanges to skip).".Expand(filename));
								performUpload = true;
							}
							else
							{
								Log.Warning("File {0} is already on server, skipped (change IgnoreInitialServerChanges to overwrite).".Expand(filename));
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
								if ((oldSyncInfo.LocalDetail != GetFileFullInfo(fullfilename)) || !config.UploadChangesOnly)
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
									Log.Warning("File {0} modified on server, overwritten (change IgnoreServerChanges to skip).".Expand(filename));
									performUpload = true;
								}
								else
								{
									Log.Warning("File {0} modified on server, skipped (change IgnoreServerChanges to overwrite).".Expand(filename));
									syncFileInfo.UpdateFtpDetail = false;
								}
							}
						}
					}

					if (performUpload)
					{
						Try(() =>
						    UploadFile(fullfilename, ftpDirectory.DirName.CombineFtp(filename)),
						    "Uploading file {0}...".Expand(filename),
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

			foreach (var fileInfo in filesToDelete)
			{
				// file was already tracked and we deleted it
				var isTracked = config.SyncInfos.Any(i => i.FtpDetail == fileInfo.FtpDetail);

				if (isTracked || _conflictResolver.DeleteFile(fileInfo.FileName, isTracked))
				{
					Try(() => DeleteFile(fileInfo.FileName),
					    "Deleting file {0}...".Expand(fileInfo.FileName));
				}
			}

			// recursively process directories on client
			var localDirs = Directory.GetDirectories(localDirectory);

			var ftpDirsToDelete = ftpDirectory.Directories.ToList();

			foreach (var localDir in localDirs)
			{
				try
				{
					var dirname = new DirectoryInfo(localDir).Name;
					var ftpFullPath = ftpDirectory.DirName.CombineFtp(dirname);
					var subdirInfo = ftpDirectory.Directories.FirstOrDefault(d => d.DirName == dirname);

					bool dirExists = true;

					if (subdirInfo == null)
					{
						// directory is not on server => create

						Try(() => MakeDirectory(
							path: ftpFullPath),
						    initialMessage: "Creating directory {0}...".Expand(dirname),
						    onError: () =>
						    	{
						    		dirExists = false;
						    	});

						//subdirInfo = new FtpDirInfo {DirName = ftpFullPath};
					}
					else
					{
						ftpDirsToDelete.Remove(subdirInfo);
					}

					if (dirExists)
					{
						Sync(localDirectory.CombinePath(dirname), dirname, ftpDirectory.DirName, config);
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
					dir =>
						{
							var fullFtpDirname = ftpDirectory.DirName.CombineFtp(dir.DirName);
							if (_conflictResolver.DeleteDirectory(fullFtpDirname))
							{
								Try(
									() => DeleteDirectory(fullFtpDirname),
									"Deleting directory {0}...".Expand(dir.DirName)
									);
							}
						});
			}

			// update ftpinfo on local settings to remember the current state for each file (not directories)
			var ftpInfos = GetDirectoryContent(ftpDirectory.DirName, false);

			// update local files state - we already have everything except ftpinfo in syncInfos
			foreach (var localInfo in localFileInfos)
			{
				var filename = localInfo.Key;
				var newSyncFileInfo = localInfo.Value;

				var ftpPath = ftpDirectory.DirName.CombineFtp(filename);

				if (newSyncFileInfo.UpdateFtpDetail)
				{
					// update ftpinfo so that we keep current state for next update
					newSyncFileInfo.FtpDetail = ftpInfos.Files.Where(f => f.FileName == ftpPath).Select(f => f.FtpDetail).FirstOrDefault();
				}
				else
				{
					// for conflicted files use old ftpinfo, so that it's used next time
					newSyncFileInfo.FtpDetail = config.SyncInfos.Where(i => i.LocalPath == newSyncFileInfo.LocalPath).Select(i => i.FtpDetail).FirstOrDefault();
				}

				config.UpdateLocalFile(newSyncFileInfo);
			}
		}

		private void Try(Action action, string initialMessage, string successMessage = "success", Action onSuccess = null, Action onError = null)
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
				Handle(e);
				if (onError != null) onError();
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
				Handle(e);
				if (onError != null) onError();
			}
		}

		private void Handle(Exception e)
		{
			Log.Error(e.Message);
			Log.Error(e.StackTrace);
		}

		private static string GetFileFullInfo(string fullfilename)
		{
			var fileinfo = new FileInfo(fullfilename);
			return "{0} {1} {2}".Expand(fileinfo.CreationTime, fileinfo.LastWriteTime, fileinfo.Length);
		}
	}
}