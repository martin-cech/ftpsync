using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace FtpSync.Utils
{
	public static class Extensions
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
			var rest = paths.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.Trim(delimiter)).JoinWith(delimiter.ToString());

			if (string.IsNullOrEmpty(path1)) return rest;

			return path1.TrimEnd(delimiter)
			       + delimiter
			       + rest;
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
			var buf = new byte[65536];
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

		public static bool SafeParseBool(this XElement xElement, string sonName, bool defaultValue = false)
		{
			if (xElement == null) return defaultValue;
			var son = xElement.Element(sonName);
			if (son == null) return defaultValue;

			bool result;
			if (bool.TryParse(son.Value, out result)) return result;

			return defaultValue;
		}

		public static string GetSonValue(this XElement element, string sonName)
		{
			if (element == null) return string.Empty;
			var son = element.Element(sonName);
			if (son == null) return string.Empty;
			return son.Value;
		}

		public static T GetAttribute<T>(this MemberInfo propertyInfo, bool inherit = true)
			where T : Attribute
		{
			return (T) propertyInfo.GetCustomAttributes(typeof (T), inherit).FirstOrDefault();
		}

		public static string GetMemberName(this Expression expression)
		{
			return expression.GetMemberInfo().Name;
		}

		public static MemberInfo GetMemberInfo(this Expression expression)
		{
			var lambda = (LambdaExpression)expression;

			MemberExpression memberExpression;
			if (lambda.Body is UnaryExpression)
			{
				var unaryExpression = (UnaryExpression)lambda.Body;
				memberExpression = (MemberExpression)unaryExpression.Operand;
			}
			else memberExpression = (MemberExpression)lambda.Body;

			return memberExpression.Member;
		}

		public static void PrintHighlighted(this string s, char c, ConsoleColor foreground = ConsoleColor.Yellow, ConsoleColor normal = ConsoleColor.Gray)
		{
			var index = s.ToLower().IndexOf(c);
			if (index < 0)
			{
				Console.ForegroundColor = normal;
				Console.Write(s);
			}
			else
			{
				Console.ForegroundColor = normal;
				if (index > 0) Console.Write(s.Substring(0, index - 1));

				Console.ForegroundColor = foreground;
				Console.Write( index == 0 ? c.ToString().ToUpper() : c.ToString());

				Console.ForegroundColor = normal;

				if (index < s.Length) Console.Write(s.Substring(index + 1));
			}
		}

		public static TValue TryGet<TKey,TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
		{
			TValue result;
			if (dictionary.TryGetValue(key, out result))
			{
				return result;
			}

			return defaultValue;
		}
	}
}