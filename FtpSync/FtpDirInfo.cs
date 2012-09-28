using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync
{
	public class FtpDirInfo
	{
		public string DirName { get; set; }

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
			AddDirectory(new FtpDirInfo { DirName = dir, FtpDetail = ftpDetail });
		}

		public void AddFile(string filename, string ftpDetail = null)
		{
			Files.Add(new FtpFileInfo { FileName = DirName.CombineFtp(filename), FtpDetail = ftpDetail });
		}

		public void AddDirectory(FtpDirInfo ftpDirInfo, string ftpDetail = null)
		{
			if (ftpDetail != null) ftpDirInfo.FtpDetail = ftpDetail;
			Directories.Add(ftpDirInfo);
		}
	}
}