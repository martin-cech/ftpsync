using System.Linq;
using System.Collections.Generic;
using System;
using FtpSync.Helpers;
using FtpSync.Utils;

namespace FtpSync.Components
{
	public class SyncFiles
	{
		private List<SyncFileInfo> _files= new List<SyncFileInfo>();

		private readonly Dictionary<string, SyncFileInfo> _byLocalPath = new Dictionary<string, SyncFileInfo>();
		private readonly Dictionary<string, SyncFileInfo> _byFtpDetail = new Dictionary<string, SyncFileInfo>();

		public void Add(SyncFileInfo info)
		{
			_byLocalPath.Add(info.LocalPath, info);
			_byFtpDetail.Add(info.FtpDetail, info);
		}

		public SyncFileInfo ByLocalPath(string localPath)
		{
			return _byLocalPath.TryGet(localPath);
		}

		public SyncFileInfo ByFtpDetail(string ftpDetail)
		{
			return _byFtpDetail.TryGet(ftpDetail);
		}
	}
}