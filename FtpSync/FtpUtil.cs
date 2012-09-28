using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace FtpSync
{
	public static class FtpUtil
	{
		public static string Expand(this string s, params object[] args)
		{
			return string.Format(s, args);
		}

		public static string JoinWith(this IEnumerable<string> strings, string delimiter)
		{
			return string.Join(delimiter, strings.ToArray());
		}

		private static string Combine(this string path1, IEnumerable<string> paths, char delimiter)
		{
			return path1.TrimEnd(delimiter) + delimiter + paths.Select(p => p.Trim(delimiter)).JoinWith(delimiter.ToString());
		}

		public static string CombineFtp(this string path1, params string[] paths)
		{
			return path1.Combine(paths, '/');
		}

		public static string CombinePath(this string path1, params string[] paths)
		{
			return path1.Combine(paths, '\\');
		}

		public static byte[] ReadAll(this Stream s)
		{
			if (s == null) return new byte[0];

			var buf = new byte[65536];
			var target = new MemoryStream();

			int len;
			while ((len = s.Read(buf, 0, buf.Length)) > 0)
			{
				target.Write(buf, 0, len);
			}

			s.Close();

			return target.ToArray();
		}

		public static long WriteAll(this Stream s, Stream target)
		{
			var buf = new byte[16384];
			long total = 0;
			int len;
			while ((len = s.Read(buf, 0, buf.Length)) > 0)
			{
				target.Write(buf, 0, len);
				total += len;
			}

			s.Close();

			return total;
		}
	}
}