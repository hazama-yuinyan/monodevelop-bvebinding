using System;

namespace BVE5Language.Parser
{
	public class BVE5ParserException : Exception
	{
		static string MsgFormat = "At line {0}, {1} : {2}";

		public BVE5ParserException(string msg) : base(msg)
		{
		}

		public BVE5ParserException(string format, params object[] values) : base(string.Format(format, values))
		{
		}

		public BVE5ParserException(int line, int column, string msg) : this(MsgFormat, line, column, msg)
		{
		}
	}
}

