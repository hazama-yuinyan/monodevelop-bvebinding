using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// Resolver logging helper.
	/// Wraps System.Diagnostics.Debug so that resolver-specific logging can be enabled/disabled on demand.
	/// (it's a huge amount of debug spew and slows down the resolver quite a bit)
	/// </summary>
	static class Log
	{
		const bool log_enabled = false;
#if __MonoCS__
		[Conditional("DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		internal static void WriteLine(string text)
		{
			Debug.WriteLine(text);
		}
		
#if __MonoCS__
		[Conditional("DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		internal static void WriteLine(string format, params object[] args)
		{
			Debug.WriteLine(format, args);
		}
		
#if __MonoCS__
		[Conditional("DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		internal static void WriteCollection<T>(string text, IEnumerable<T> lines)
		{
#if DEBUG
			T[] arr = lines.ToArray();
			if(arr.Length == 0){
				Debug.WriteLine(text + "<empty collection>");
			}else{
				Debug.WriteLine(text + (arr[0] != null ? arr[0].ToString() : "<null>"));
				for(int i = 1; i < arr.Length; i++)
					Debug.WriteLine(new string(' ', text.Length) + (arr[i] != null ? arr[i].ToString() : "<null>"));
			}
#endif
		}
		
#if __MonoCS__
		[Conditional("DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		public static void Indent()
		{
			Debug.Indent();
		}
		
#if __MonoCS__
		[Conditional("DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		public static void Unindent()
		{
			Debug.Unindent();
		}
	}
}

