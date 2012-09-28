using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync
{
	public class FtpDirInfo
	{
		public string FullPath { get; set; }

		public string FtpDetail { get; set; }

		public List<FtpFileInfo> Files { get; private set; }
		public List<FtpDirInfo> Directories { get; private set; }

		public bool IsEmpty()
		{
			return Files.Count == 0 && Directories.Count == 0;
		}

		public FtpDirInfo()
		{
			Files = new List<FtpFileInfo>();
			Directories = new List<FtpDirInfo>();
		}

		public void AddDirectory(string dir, string ftpDetail = null)
		{
			AddDirectory(new FtpDirInfo { FullPath = this.FullPath.CombineFtp(dir), FtpDetail = ftpDetail });
		}

		public void AddFile(string filename, string ftpDetail = null)
		{
			Files.Add(new FtpFileInfo { FileName = FullPath.CombineFtp(filename), FtpDetail = ftpDetail });
		}

		public void AddDirectory(FtpDirInfo ftpDirInfo, string ftpDetail = null)
		{
			if (ftpDetail != null) ftpDirInfo.FtpDetail = ftpDetail;
			Directories.Add(ftpDirInfo);
		}

		public override string ToString()
		{
			return "{0} ({1} files, {2} subdirs)".Expand(FullPath, Files.Count, Directories.Count);
		}
	}
}