//
// AstNode.cs
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
using System.Collections.Generic;

using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Base class for all the AST nodes.
	/// </summary>
	public abstract class AstNode : AbstractAnnotatable
	{
		protected readonly TextLocation start_loc, end_loc;
		private AstNode parent, prev_sibling, next_sibling, first_child, last_child;

		public TextLocation StartLocation{
			get{return start_loc;}
		}

		public TextLocation EndLocation{
			get{return end_loc;}
		}

		public AstNode Parent{
			get{return parent;}
		}

		public AstNode(TextLocation startLoc, TextLocation endLoc)
		{
			start_loc = startLoc;
			end_loc = endLoc;
		}

		/// <summary>
		/// Accepts the ast walker.
		/// </summary>
		/// <param name='walker'>
		/// Walker.
		/// </param>
		public abstract void AcceptWalker(AstWalker walker);
		public abstract TResult AcceptWalker<TResult>(IAstWalker<TResult> walker);
		/// <summary>
		/// Gets the text representation of the node.
		/// </summary>
		/// <returns>
		/// The text.
		/// </returns>
		public abstract string GetText();

		#region Node factories
		/*public static CommentNode MakeComment(string content, TextLocation start, TextLocation end)
		{
			return new CommentNode(content, start, end);
		}*/

		public static Identifer MakeIdent(string name, TextLocation start, TextLocation end)
		{
			return new Identifer(name, start, end);
		}

		public static IndexerExpression MakeIndexExpr(Expression target, Identifer key, TextLocation start, TextLocation end)
		{
			return new IndexerExpression(target, key, start, end);
		}

		public static InvocationExpression MakeInvoke(Expression invokeTarget, List<Expression> args, TextLocation start,
		                                              TextLocation end)
		{
			return new InvocationExpression(invokeTarget, args.ToArray(), start, end);
		}

		public static LiteralExpression MakeLiteral(object value, TextLocation start, TextLocation end)
		{
			return new LiteralExpression(value, start, end);
		}

		public static MemberReferenceExpression MakeMemRef(Expression target, Identifer reference, TextLocation start, TextLocation end)
		{
			return new MemberReferenceExpression(target, reference, start, end);
		}

		public static Statement MakeStatement(Expression expr, TextLocation start, TextLocation end)
		{
			return new Statement(expr, start, end);
		}

		public static SyntaxTree MakeSyntaxTree(List<Statement> body, string name, TextLocation start, TextLocation end)
		{
			return new SyntaxTree(body.ToArray(), name, start, end);
		}

		public static TimeFormatLiteral MakeTimeFormat(int hour, int min, int sec, TextLocation start, TextLocation end)
		{
			return new TimeFormatLiteral(hour, min, sec, start, end);
		}
		#endregion
	}
}

