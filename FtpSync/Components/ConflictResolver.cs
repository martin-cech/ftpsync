using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FtpSync.Helpers;
using FtpSync.Utils;

namespace FtpSync.Components
{
	public class ConflictResolver
	{
		private readonly Configuration _configuration;

		public ConflictResolver(Configuration configuration)
		{
			_configuration = configuration;
		}

		private bool Ask(string question, IEnumerable<Answer> answers)
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(question);
			Console.Write(" (");

			bool printComma = false;
			foreach (var answer in answers)
			{
				if (answer == Answer.Ask) continue;
				
				if (printComma) Console.Write(", ");

				var shortcut = answer.GetShortcut();
				answer.ToString().PrintHighlighted(shortcut);
				
				printComma = true;
			}

			Console.Write("): ");

			bool? result = null;

			while(true)
			{
				var s = Console.ReadKey().KeyChar;

				switch (s)
				{
					case 'y':
						result = true;
						break;
					case 'n':
						result = false;
						break;
						// TODO: think about other answers, like yes/no to all/all in current folder/ignore/download/ ...
				}

				if (result != null)
				{
					Console.WriteLine();
					return result.Value;
				}
			}
		}

		private bool GetAnswer(Expression<Func<Configuration, Answer>> expression, string question)
		{
			var configAnswer = expression.Compile()(_configuration);
			var answers = typeof (Configuration).GetProperty(expression.GetMemberName()).GetAttribute<AnswersAttribute>().Answers;

			switch (configAnswer)
			{
				case Answer.Yes:
					return true;
				case Answer.No:
					return false;
				case Answer.Ask:
					return Ask(question, answers);
				default:
					throw new NotImplementedException();
			}
		}

		public bool OverwriteServerModified(string filename)
		{
			return GetAnswer(c => c.OverwriteServerChangedFiles, "File {0} has been modified on server, overwrite?".Expand(filename));
		}

		public bool OverwriteUntrackedFile(string filename)
		{
			return GetAnswer(c => c.OverwriteUntrackedFiles, "File {0} is already on server but we don't know anything about it. Overwrite with your local file?".Expand(filename));
		}

		public bool DeleteFile(string fileName, bool isTracked)
		{
			return
				isTracked
					? GetAnswer(c => c.DeleteTrackedFiles, "File {0} was locally deleted, delete on server?".Expand(fileName))
					: GetAnswer(c => c.DeleteUntrackedFiles, "File {0} is on server, but has never been on client. Delete?".Expand(fileName));
		}

		public bool DeleteDirectory(string path, bool isTracked)
		{
			return
				isTracked
					? GetAnswer(c => c.DeleteTrackedDirectories, "Directory {0} was locally deleted, delete on server?".Expand(path))
					: GetAnswer(c => c.DeleteUntrackedDirectories, "Directory {0} is on server, but has never been on client. Delete?".Expand(path));
		}
	}
}