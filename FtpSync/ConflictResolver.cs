using System;

namespace FtpSync
{
	public interface IConflictResolver
	{
		bool OverwriteServerModified(string filename);
		bool OverwriteFileWhenNoLocalInfo(string filename);
		bool DeleteFile(string fileName, bool isTracked);
		bool DeleteDirectory(string dirName);
	}

	public class CuriousConflictResolver : IConflictResolver
	{
		private bool Ask(string message)
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(message + (" (y/n)? "));
			var s = Console.ReadLine();
			return (s == "y") || (s == "Y");
		}

		public bool OverwriteServerModified(string filename)
		{
			return Ask("File {0} has been modified on server, overwrite".Expand(filename));
		}

		public bool OverwriteFileWhenNoLocalInfo(string filename)
		{
			return Ask("File {0} is already on server but we don't know anything about it. Overwrite with your local file".Expand(filename));
		}

		public bool DeleteFile(string fileName, bool isTracked)
		{
			var question = isTracked
			               	? "File {0} was locally deleted, delete on server".Expand(fileName)
			               	: "File {0} is on server, but has never been on client. Delete".Expand(fileName);

			return Ask(question);
		}

		public bool DeleteDirectory(string dirName)
		{
			return Ask("Directory {0} was locally deleted, delete on server".Expand(dirName));
		}
	}

	public class DefaultConflictResolver : IConflictResolver
	{
		private readonly Configuration _configuration;

		public DefaultConflictResolver(Configuration configuration)
		{
			_configuration = configuration;
		}

		bool IConflictResolver.OverwriteServerModified(string filename)
		{
			return _configuration.IgnoreServerChanges;
		}

		public bool OverwriteFileWhenNoLocalInfo(string filename)
		{
			return _configuration.IgnoreInitialServerChanges;
		}

		public bool DeleteFile(string fileName, bool isTracked)
		{
			return !_configuration.KeepNonexistingLocalFilesOnServer;
		}

		public bool DeleteDirectory(string dirName)
		{
			return !_configuration.KeepNonexistingLocalFoldersOnServer;
		}
	}
}