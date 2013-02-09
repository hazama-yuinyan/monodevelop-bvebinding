//
// InvocationExpression.cs
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
using System.Text;

using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents an invoke expression.
	/// </summary>
	public class InvocationExpression : Expression
	{
		private readonly Expression target;
		private readonly Expression[] args;

		public Expression Target{
			get{return target;}
		}

		public Expression[] Arguments{
			get{return args;}
		}

		public override NodeType Type {
			get {
				return NodeType.Invocation;
			}
		}

		public InvocationExpression(Expression targetExpr, Expression[] arguments, TextLocation startLoc, TextLocation endLoc)
			: base(startLoc, endLoc)
		{
			target = targetExpr;
			args = arguments;
		}

		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				target.AcceptWalker(walker);
				foreach(var arg in args)
					arg.AcceptWalker(walker);
			}
			walker.PostWalk(this);
		}

		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}

		public override string GetText()
		{
			var sb = new StringBuilder(target.GetText() + "(");
			foreach(var arg in args)
				sb.Append(arg.GetText());

			sb.Append(")");
			return sb.ToString();
		}
	}
}

