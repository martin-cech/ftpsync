using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync
{
	public static class Log
	{
		private enum Severity
		{
			Verbose,
			Info,
			Warning,
			Error
		}

		private static void Write(Severity severity, string message)
		{
			switch (severity)
			{
				case Severity.Verbose:
					Console.ForegroundColor = ConsoleColor.Gray;
					break;
				case Severity.Info:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case Severity.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case Severity.Error:
					Console.ForegroundColor = ConsoleColor.Magenta;
					break;
			}

			Console.WriteLine(message);
		}

		public static void Verbose(string message)
		{
			Write(Severity.Verbose, message);
		}

		public static void Info(string message)
		{
			Write(Severity.Info, message);
		}

		public static void Warning(string message)
		{
			Write(Severity.Warning, message);
		}

		public static void Error(string message)
		{
			Write(Severity.Error, message);
		}
	}
}