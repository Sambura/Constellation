using System;
using System.Linq;

namespace Core
{
	public class Utility
	{
		public static readonly Type[] IntegralTypes = new[] { typeof(int), typeof(uint),
													   typeof(short), typeof(ushort),
													   typeof(long), typeof(ulong),
													   typeof(byte) };

		/// <summary>
		/// Tells if the given type is integral (only default types are checked)
		/// 
		/// See IntegralTypes array for details
		/// </summary>
		public static bool IsIntegral(Type type)
		{
			return IntegralTypes.Contains(type);
		}

		/// <summary>
		/// Splits camel case string on words making all letters lowercase except for the first one
		/// </summary>
		public static string SplitAndLowerCamelCase(string str)
		{
			return str[0] + SplitCamelCase(str).ToLowerInvariant().Substring(1);
		}

		/// <summary>
		/// Splits camel case string by adding spaces between the words
		/// 
		/// https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
		/// </summary>
		public static string SplitCamelCase(string str)
		{
			return System.Text.RegularExpressions.Regex.Replace(
				System.Text.RegularExpressions.Regex.Replace(
					str,
					@"(\P{Ll})(\P{Ll}\p{Ll})",
					"$1 $2"
				),
				@"(\p{Ll})(\P{Ll})",
				"$1 $2"
			);
		}
	}
}
