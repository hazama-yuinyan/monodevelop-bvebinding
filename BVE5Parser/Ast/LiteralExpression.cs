//
// LiteralExpression.cs
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
	/// Represents a literal like "10" or "10.5".
	/// </summary>
	public class LiteralExpression : Expression
	{
		private readonly object value;

		public object Value{
			get{return value;}
		}

		public override NodeType Type {
			get {
				return NodeType.Literal;
			}
		}

		public LiteralExpression(object parsedValue, TextLocation startLoc, TextLocation endLoc)
			: base(startLoc, endLoc)
		{
			value = parsedValue;
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
			return value.ToString();
		}
	}
}

