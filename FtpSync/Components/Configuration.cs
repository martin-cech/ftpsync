using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using FtpSync.Helpers;
using FtpSync.Utils;

namespace FtpSync.Components
{
	public class Configuration
	{
		public string ServerRoot { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string LocalFolder { get; set; }

		[Answers(Answer.Yes, Answer.No, Answer.Ask)]
		[DefaultValue(Answer.Yes)]
		public Answer DeleteTrackedFiles { get; set; }

		[Answers(Answer.Yes, Answer.No, Answer.Ask)]
		[DefaultValue(Answer.No)]
		public Answer DeleteUntrackedFiles { get; set; }

		[Answers(Answer.Yes, Answer.No, Answer.Ask)]
		[DefaultValue(Answer.Ask)]
		public Answer OverwriteUntrackedFiles { get; set; }

		[Answers(Answer.Yes, Answer.No, Answer.Ask)]
		[DefaultValue(Answer.Yes)]
		public Answer DeleteTrackedDirectories { get; set; }

		[Answers(Answer.Yes, Answer.No, Answer.Ask)]
		[DefaultValue(Answer.Ask)]
		public Answer DeleteUntrackedDirectories { get; set; }

		[Answers(Answer.Yes, Answer.No, Answer.Ask)]
		[DefaultValue(Answer.Ask)]
		public Answer OverwriteServerChangedFiles { get; set; }

		[DefaultValue(true)]
		public bool UploadChangesOnly { get; set; }

		public static Configuration Default
		{
			get
			{
				var configuration = new Configuration();

				foreach (var prop in typeof(Configuration).GetProperties(BindingFlags.SetProperty | BindingFlags.Instance |BindingFlags.Public))
				{
					var attr = prop.GetAttribute<DefaultValueAttribute>();
					if (attr == null) continue;
					prop.SetValue(configuration, attr.Value, new object[0]);
				}

				return configuration;
			}
		}
	}
}