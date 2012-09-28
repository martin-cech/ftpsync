using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync
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