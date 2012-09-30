using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync.Helpers
{
	public enum Answer
	{
		[Shortcut('y')]
		Yes = 0x01,

		[Shortcut('n')]
		No = 0x02,

		[Shortcut('a')]
		Ask = 0x04,

		// TODO: ignore, synchronize, download ... ?
	}
}