using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync.Helpers
{
	public class SyncFileInfo
	{
		public string LocalPath { get; set; }
		public string LocalDetail { get; set; }
		public string FtpDetail { get; set; }

		public bool UpdateFtpDetail { get; set; }
	}
}