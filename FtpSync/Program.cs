using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FtpSync.Components;

namespace FtpSync
{
	class Program
	{
		static void Main(string[] args)
		{
			//args = new[] {@"c:\Users\Pz\Documents\dev\praettest.cfg"};
			args = new[] {@"c:\Users\Pz\Documents\dev\sync.cfg"};

			var configuration = ParseArgs(args);

			if (configuration == null)
			{
				PrintUsage();
			}
			else
			{
				var trackedFilesPath = args[0];

				try
				{
					var ftp = new FtpClient(trackedFilesPath, configuration);
					ftp.Synchronize();
				}
				catch (Exception e)
				{
					if (!FtpClient.Handle(e)) throw;
				}
			}

			Console.WriteLine("");
			Console.WriteLine("Press any key to quit...");
			Console.ReadKey();

		}

		private static Configuration ParseArgs(string[] args)
		{
			if (args.Length == 0) return null;

			// TODO: parse args
			var configuration = Configuration.Default;
			configuration.Username = "ftptest";
			configuration.Password = "ftptest";
			configuration.LocalFolder = @"c:\Users\Pz\Documents\dev\synctest\";
			configuration.ServerRoot = @"ftp://127.0.0.1/root";

			return configuration;
		}

		private static void PrintUsage()
		{
			Console.WriteLine("Welcome to FtpSync.");
			Console.WriteLine("Usage:");
			Console.WriteLine("");
			Console.WriteLine("  ftpsync.exe configfile");
			Console.WriteLine("");


			var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var cfgPath = Path.Combine(dir, "default.cfg");

			if (!File.Exists(cfgPath))
			{
				TrackedFiles.Create(cfgPath).Save();
			}

			/*
			if (!File.Exists(cfgPath))
			{
				new Configuration()
					{
						Username = "your login here",
						Password = "guess what here",
						ServerRoot = "www.your-damned-ftp.com/rootfolder/likethis",
						LocalFolder = @"x:\bloodycustomer\uselessweb",

						AskOnConflict = true,
						KeepNonexistingLocalFilesOnServer = true,
						KeepNonexistingLocalFoldersOnServer = true,
						IgnoreInitialServerChanges = false,
						IgnoreServerChanges = false,
						UploadChangesOnly = true
					};
			}*/
		}
	}
}
