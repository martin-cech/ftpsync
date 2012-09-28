using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync
{
	public class FtpDirInfo
	{
		public string DirName { get; set; }
		public string FullInfo { get; set; }

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

		public void AddDirectory(string dir, string fullInfo = null)
		{
			AddDirectory(new FtpDirInfo { DirName = dir, FullInfo = fullInfo });
		}

		public void AddFile(string filename, string fullInfo = null)
		{
			Files.Add(new FtpFileInfo { FileName = DirName.CombineFtp(filename), FtpDetail = fullInfo });
		}

		public void AddDirectory(FtpDirInfo ftpDirInfo, string fullInfo = null)
		{
			if (fullInfo != null) ftpDirInfo.FullInfo = fullInfo;
			Directories.Add(ftpDirInfo);
		}
	}
}