using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync.Utils
{
	[Flags]
	public enum LogOptions
	{
		None = 0x00,
		NoNewLine = 0x01
	}
}