﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FtpSync
{
	class Program
	{
		static void Main(string[] args)
		{
			args = new[] {@"c:\Users\Pz\Documents\dev\sync.cfg"};

			if (!ParseArgs(args))
			{
				PrintUsage();
			}
			else
			{
				var configPath = args[0];

				try
				{
					var ftp = new FtpClient(configPath);
					ftp.Synchronize();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
			}

			Console.WriteLine("");
			Console.WriteLine("Press any key to quit...");
			Console.ReadKey();

		}

		private static bool ParseArgs(string[] args)
		{
			if (args.Length == 0) return false;
			if (!File.Exists(args[0]))
			{
				Console.WriteLine("File {0} does not exist.".Expand(args[0]));
				return false;
			}

			return true;
		}

		private static void PrintUsage()
		{
			Console.WriteLine("Welcome to FtpSync.");
			Console.WriteLine("Usage is quite hard to understand, but don't underestimate yourself!");
			Console.WriteLine("");
			Console.WriteLine("\tftpsync.exe configfile");
			Console.WriteLine("");
			Console.WriteLine("...yep, that's it. To see default config file, see default.cfg. Copy that, rewrite login info and use. Enjoy your life. Hunt a snowboarder or something.");
			Console.WriteLine("");

			var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var cfgPath = Path.Combine(dir, "default.cfg");
			if (!File.Exists(cfgPath))
			{
				Console.WriteLine("The config file has been created.");

				new Configuration
					{
						Username = "your login here",
						Password = "guess what here",
						ServerRoot = "www.your-damned-ftp.com/rootfolder/likethis",
						LocalFolder = @"x:\bloodycustomer\uselessweb",

						KeepNonexistingLocalFilesOnServer = true,
						KeepNonexistingLocalFoldersOnServer = true,
						IgnoreInitialServerChanges = false,
						IgnoreServerChanges = false,
						UploadChangesOnly = true
					}.SaveToFile(cfgPath);
			}
		}
	}
}
