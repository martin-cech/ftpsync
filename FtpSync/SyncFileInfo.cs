using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync
{
	public class SyncFileInfo
	{
		public string LocalPath { get; set; }
		public string LocalInfo { get; set; }
		public string FtpInfo { get; set; }
	}
}