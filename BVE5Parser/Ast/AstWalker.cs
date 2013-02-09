//
// AstWalker.cs
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

namespace BVE5Language.Ast
{
	/// <summary>
	/// An ast walker.(default return value is true)
	/// </summary>
	public class AstWalker
	{
		//protected virtual bool Walk(AstNode node){return true;}

		public virtual bool Walk(Expression node){return true;}
		public virtual void PostWalk(Expression node){}

		public virtual bool Walk(Identifier node){return true;}
		public virtual void PostWalk(Identifier node){}

		public virtual bool Walk(IndexerExpression node){return true;}
		public virtual void PostWalk(IndexerExpression node){}

		public virtual bool Walk(InvocationExpression node){return true;}
		public virtual void PostWalk(InvocationExpression node){}

		public virtual bool Walk(LiteralExpression node){return true;}
		public virtual void PostWalk(LiteralExpression node){}

		public virtual bool Walk(MemberReferenceExpression node){return true;}
		public virtual void PostWalk(MemberReferenceExpression node){}

		public virtual bool Walk(Statement node){return true;}
		public virtual void PostWalk(Statement node){}

		public virtual bool Walk(SyntaxTree node){return true;}
		public virtual void PostWalk(SyntaxTree node){}

		public virtual bool Walk(TimeFormatLiteral node){return true;}
		public virtual void PostWalk(TimeFormatLiteral node){}
	}

	public interface IAstWalker<TResult>
	{
		TResult Walk(Identifier node);
		TResult Walk(IndexerExpression node);
		TResult Walk(InvocationExpression node);
		TResult Walk(LiteralExpression node);
		TResult Walk(MemberReferenceExpression node);
		TResult Walk(Statement node);
		TResult Walk(SyntaxTree node);
		TResult Walk(TimeFormatLiteral node);
	}
}

