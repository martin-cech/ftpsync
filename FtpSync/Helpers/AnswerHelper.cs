using System.Linq;
using System.Collections.Generic;
using System;
using FtpSync.Utils;

namespace FtpSync.Helpers
{
	public static class AnswerHelper
	{
		private static readonly Dictionary<Answer, char> Shortcuts = new Dictionary<Answer, char>();

		static AnswerHelper()
		{
			foreach (Answer answer in Enum.GetValues(typeof(Answer)))
			{
				var shortcut = answer.GetType().GetField(answer.ToString()).GetAttribute<ShortcutAttribute>().Char;
				Shortcuts.Add(answer, shortcut);
			}
		}

		public static char GetShortcut(this Answer answer)
		{
			return Shortcuts[answer];
		}
	}
}