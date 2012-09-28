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

		public FtpClient(string configPath)
		{
			_configPath = configPath;
			_configuration = Configuration.LoadFromFile(_configPath);
			_credentials = new NetworkCredential(_configuration.Username, _configuration.Password);
		}

		public void MakeDirectory(string path)
		{
			Execute(WebRequestMethods.Ftp.MakeDirectory, path);
		}

		public void DeleteEmptyDirectory(string path)
		{
			Execute(WebRequestMethods.Ftp.RemoveDirectory, path);
		}

		public void DeleteDirectory(FtpDirInfo dirInfo, bool deleteRoot = true)
		{
			if (!dirInfo.IsEmpty())
			{
				foreach (var file in dirInfo.Files)
				{
					DeleteFile(file.FileName);
				}

				foreach (var dir in dirInfo.Directories)
				{
					DeleteDirectory(dir);
				}
			}

			if (deleteRoot) DeleteEmptyDirectory(dirInfo.DirName);
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

		public FtpDirInfo GetDirectoryContent(string path, bool recursive = true)
		{
			var filelist = GetFileList(path);
			var details = GetFileListDetail(path);

			var result = new FtpDirInfo {DirName = path};

			foreach (var file in filelist)
			{
				var fullInfo = details.FirstOrDefault(dir => dir.EndsWith(" " + file));
				if (fullInfo == null) continue;

				if (fullInfo.StartsWith("d"))
				{
					if (recursive)
					{
						result.AddDirectory(GetDirectoryContent(path.CombineFtp(file)), fullInfo);
					}
					else
					{
						result.AddDirectory(file, fullInfo);
					}
				}
				else
				{
					result.AddFile(file, fullInfo);
				}
			}

			return result;
		}

		public string[] GetFileList(string path)
		{
			return ExecuteStreamString(WebRequestMethods.Ftp.ListDirectory, path).Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
		}

		public string[] GetFileListDetail(string path)
		{
			return ExecuteStreamString(WebRequestMethods.Ftp.ListDirectoryDetails, path).Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
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
			Sync(_configuration.LocalFolder, GetDirectoryContent(_configuration.ServerRoot), _configuration);
			_configuration.SaveToFile(_configPath);
		}

		private void Sync(string localDirectory, FtpDirInfo dirInfo, Configuration config)
		{
			var files = Directory.GetFiles(localDirectory);
			var syncInfos = config.SyncInfos;

			var filesToDelete = dirInfo.Files.ToList();

			var localFileInfos = new Dictionary<string, SyncFileInfo>();
			var conflictedFiles = new List<FtpFileInfo>();

			// check all files in current directory
			foreach (var fullfilename in files)
			{
				var filename = Path.GetFileName(fullfilename);
				var fullLocalPath = localDirectory.CombinePath(filename);
				var syncFileInfo = new SyncFileInfo {LocalPath = fullLocalPath, LocalInfo = GetFileFullInfo(fullfilename)};

				localFileInfos.Add(filename, syncFileInfo);

				var syncInfo = syncInfos.FirstOrDefault(i => i.LocalPath == fullLocalPath);
				var ftpInfo = dirInfo.Files.FirstOrDefault(i => i.FileName == dirInfo.DirName.CombineFtp(filename));

				bool performUpload = false;

				filesToDelete.Remove(ftpInfo);


				// is file already known to client?
				if (syncInfo == null)
				{
					if (ftpInfo == null)
					{
						// file is not on server
						performUpload = true;
					}
					else
					{
						// file is already on server - depends on settings now
						if (config.IgnoreInitialServerChanges)
						{
							Log.Warning("File {0} is already on server, but since IgnoreInitialServerChanges is true, the file will be overriden.");
							performUpload = true;
						}
						else
						{
							Log.Warning("File {0} is already on server, but since IgnoreInitialServerChanges is false, the file will be skipped.");
							conflictedFiles.Add(ftpInfo);
						}
					}
				}
				else
				{
					if (ftpInfo == null)
					{
						// file is not on server -> upload
						performUpload = true;
					}
					else
					{
						if (syncInfo.FtpInfo != ftpInfo.FullInfo)
						{
							// TODO: state, when file is changed on both sides is now considered a conflict

							if (string.IsNullOrEmpty(syncInfo.FtpInfo))
							{
								// file is on server, but we don't have the info => upload
								if (config.IgnoreInitialServerChanges)
								{
									Log.Warning("File {0} is on server, overriden.".Expand(filename));
									performUpload = true;
								}
								else
								{
									Log.Warning("File {0} is already on the server, not uploading (empty local info).".Expand(filename));
									conflictedFiles.Add(ftpInfo);
								}
							}
							else
							{
								if (config.IgnoreServerChanges)
								{
									Log.Warning("File {0} has been modified on server, overriden.".Expand(filename));
									performUpload = true;
								}
								else
								{
									Log.Warning("File {0} has already been modified on server.".Expand(filename));
									conflictedFiles.Add(ftpInfo);
								}
							}
						}
						else
						{
							// file has changed or we upload all files -> upload
							if ((syncInfo.LocalInfo != GetFileFullInfo(fullfilename)) || !config.UploadChangesOnly)
							{
								performUpload = true;
							}
							else
							{
								Log.Verbose("File not changed ({0})".Expand(filename));
							}
						}
					}
				}

				if (performUpload)
				{
					var len = UploadFile(fullfilename, dirInfo.DirName.CombineFtp(filename));
					Log.Info("File {0} uploaded ({1} bytes)".Expand(filename, len));
				}
			}

			// delete those ftp files, that are not present on client (optional)
			if (!config.KeepNonexistingLocalFilesOnServer)
			{
				foreach (var fileInfo in filesToDelete)
				{
					Log.Info("File {0} deleted.".Expand(fileInfo.FileName));
					DeleteFile(fileInfo.FileName);
				}
			}

			// recursively process directories on client
			var dirs = Directory.GetDirectories(localDirectory);

			var unprocessedFtpDirs = dirInfo.Directories.ToList();

			foreach (var fulldirname in dirs)
			{
				var dirname = new DirectoryInfo(fulldirname).Name;
				var ftpFullPath = dirInfo.DirName.CombineFtp(dirname);
				var subdirInfo = dirInfo.Directories.FirstOrDefault(d => d.DirName == ftpFullPath);

				if (subdirInfo == null)
				{
					// directory is not on server => create
					Log.Info("Directory {0} created".Expand(ftpFullPath));
					MakeDirectory(dirInfo.DirName.CombineFtp(dirname));
					subdirInfo = new FtpDirInfo {DirName = ftpFullPath};
				}
				else
				{
					unprocessedFtpDirs.Remove(subdirInfo);
				}

				Sync(localDirectory.CombinePath(dirname), subdirInfo, config);
			}

			// delete ftp directories which have been deleted on client (optional)
			if (!config.KeepNonexistingLocalFoldersOnServer)
			{
				foreach (var dir in unprocessedFtpDirs)
				{
					Log.Info("Directory {0} deleted.".Expand(dir.DirName));
					DeleteDirectory(dir);
				}
			}

			// update ftpinfo on local settings to remember the current state for each file (not directories)
			var ftpInfos = GetDirectoryContent(dirInfo.DirName, false);

			foreach (var localInfo in localFileInfos)
			{
				// skip files that were not uploaded
				var ftpPath = dirInfo.DirName.CombineFtp(localInfo.Key);
				//if (filesToDelete.Any(fi => fi.FileName == ftpPath)) continue;

				var conflictedFile = conflictedFiles.FirstOrDefault(cf => cf.FileName == ftpPath);

				if (conflictedFile == null)
				{
					// update ftpinfo so that we keep current state for next update
					localInfo.Value.FtpInfo = ftpInfos.Files.Where(f => f.FileName == ftpPath).Select(f => f.FullInfo).FirstOrDefault();
				}
				else
				{
					// for conflicted files use old ftpinfo, so that it's used next time
					localInfo.Value.FtpInfo = config.SyncInfos.Where(i => i.LocalPath == localInfo.Value.LocalPath).Select(i => i.FtpInfo).FirstOrDefault();
				}

				config.UpdateLocalFile(localInfo.Value);
			}

			// and that's it, incremental sync at its finest
		}

		private string GetFileFullInfo(string fullfilename)
		{
			var fileinfo = new FileInfo(fullfilename);
			return "{0} {1}".Expand(fileinfo.CreationTime, fileinfo.Length);
		}
	}
}