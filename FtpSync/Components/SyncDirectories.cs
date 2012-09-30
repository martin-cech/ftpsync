using System.Linq;
using System.Collections.Generic;
using System;
using FtpSync.Helpers;
using FtpSync.Utils;

namespace FtpSync.Components
{
	public class SyncDirectories
	{
		private readonly List<SyncDirInfo> _directories = new List<SyncDirInfo>();

		private readonly Dictionary<string, SyncDirInfo> _byLocalPath = new Dictionary<string, SyncDirInfo>();
		private readonly Dictionary<string, SyncDirInfo> _byFtpPath = new Dictionary<string, SyncDirInfo>();

		public void Add(SyncDirInfo info)
		{
			_byLocalPath.Add(info.LocalPath, info);
			_byFtpPath.Add(info.FtpPath, info);
		}

		public SyncDirInfo ByLocalPath(string localPath)
		{
			return _byLocalPath.TryGet(localPath);
		}

		public SyncDirInfo ByFtpPath(string ftpDetail)
		{
			return _byFtpPath.TryGet(ftpDetail);
		}

		public IEnumerable<SyncDirInfo> GetAll()
		{
			return _directories;
		}
	}
}