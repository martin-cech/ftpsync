using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync.Utils
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

		private static void Write(Severity severity, string message, LogOptions logOptions)
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

	
			if ((logOptions & LogOptions.NoNewLine) == LogOptions.NoNewLine)
			{
				Console.Write(message);
			}
			else
			{
				Console.WriteLine(message);
			}

			Console.ForegroundColor = ConsoleColor.Gray;
		}

		public static void Verbose(string message, LogOptions logOptions = LogOptions.None)
		{
			Write(Severity.Verbose, message, logOptions);
		}

		public static void Info(string message, LogOptions logOptions = LogOptions.None)
		{
			Write(Severity.Info, message, logOptions);
		}

		public static void Warning(string message, LogOptions logOptions = LogOptions.None)
		{
			Write(Severity.Warning, message, logOptions);
		}

		public static void Error(string message, LogOptions logOptions = LogOptions.None)
		{
			Write(Severity.Error, message, logOptions);
		}
	}
}