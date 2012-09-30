using System.Linq;
using System.Collections.Generic;
using System;
using FtpSync.Utils;

namespace FtpSync.Helpers
{
	public class FtpFileInfo
	{
		public string FileName { get; set; }
		public string FtpDetail { get; set; }

		public override string ToString()
		{
			return "{0} ({1})".Expand(FileName, FtpDetail);
		}
	}
}