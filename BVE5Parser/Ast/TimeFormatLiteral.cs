//
// TimeFormatLiteral.cs
//
// Author:
//       HAZAMA <kotonechan@live.jp>
//
// Copyright (c) 2013 HAZAMA
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a time format literal like "20:10:30".
	/// </summary>
	public class TimeFormatLiteral : Expression
	{
		private readonly int hour, minute, second;

		public int Hour{
			get{return hour;}
		}

		public int Minute{
			get{return minute;}
		}

		public int Second{
			get{return second;}
		}

		public override NodeType Type {
			get {
				return NodeType.TimeLiteral;
			}
		}

		public TimeFormatLiteral(int inputHour, int inputMin, int inputSec, TextLocation startLoc, TextLocation endLoc)
			: base(startLoc, endLoc)
		{
			hour = inputHour;
			minute = inputMin;
			second = inputSec;
		}

		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){}
			walker.PostWalk(this);
		}

		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}

		public override string GetText()
		{
			return string.Format("<Literal {0}:{1}:{2}>", hour, minute, second);
		}
	}
}

