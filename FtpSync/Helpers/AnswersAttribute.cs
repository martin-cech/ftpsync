using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync.Helpers
{
	public class AnswersAttribute : Attribute
	{
		public Answer[] Answers { get; set; }

		public AnswersAttribute(params Answer[] answers)
		{
			Answers = answers;
		}
	}
}