using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync.Helpers
{
	public class ShortcutAttribute : Attribute
	{
		public char Char { get; private set; }

		public ShortcutAttribute(char c)
		{
			Char = c;
		}
	}
}