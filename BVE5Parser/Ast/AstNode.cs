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
	public abstract class AstNode
	{
		protected readonly TextLocation start_loc, end_loc;
		private AstNode parent, prev_sibling, next_sibling, first_child, last_child;

		#region Properties
		public TextLocation StartLocation{
			get{return start_loc;}
		}

		public TextLocation EndLocation{
			get{return end_loc;}
		}

		public AstNode Parent{
			get{return parent;}
		}

		public AstNode NextSibling{
			get{return next_sibling;}
		}

		public AstNode PrevSibling{
			get{return prev_sibling;}
		}

		public AstNode FirstChild{
			get{return first_child;}
		}

		public AstNode LastChild{
			get{return last_child;}
		}

		/// <summary>
		/// Traverses each child node.
		/// </summary>
		/// <value>
		/// The children.
		/// </value>
		public IEnumerable<AstNode> Children{
			get{
				AstNode node = first_child;
				while(node != null){
					yield return node;
					node = node.first_child;
				}
			}
		}

		public IEnumerable<AstNode> Siblings{
			get{
				AstNode node = next_sibling;
				while(node != null){
					yield return node;
					node = node.next_sibling;
				}
			}
		}

		public abstract NodeType Type{
			get;
		}
		#endregion

		protected AstNode(TextLocation startLoc, TextLocation endLoc)
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

		#region CommonAstNode methods
		public bool Contains(TextLocation loc)
		{
			return start_loc < loc && loc < end_loc;
		}

		public void AddChild<T>(T child) where T : AstNode
		{
			if(child == null)
				return;

			if(child.parent != null)
				throw new ArgumentException("Node is already used in another tree.", "child");

			AddChildUnsafe(child);
		}
		
		/// <summary>
		/// Adds a child without performing any safety checks.
		/// </summary>
		void AddChildUnsafe(AstNode child)
		{
			child.parent = this;
			if(first_child == null){
				last_child = first_child = child;
			}else{
				last_child.next_sibling = child;
				child.prev_sibling = last_child;
				last_child = child;
			}
		}

		#region GetNodeAt
		/// <summary>
		/// Gets the node specified by T at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public AstNode GetNodeAt(int line, int column, Predicate<AstNode> pred = null)
		{
			return GetNodeAt(new TextLocation(line, column), pred);
		}
		
		/// <summary>
		/// Gets the node specified by pred at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public AstNode GetNodeAt(TextLocation location, Predicate<AstNode> pred = null)
		{
			AstNode result = null;
			AstNode node = this;
			while(node.FirstChild != null){
				var child = node.FirstChild;
				while(child != null){
					if(child.StartLocation <= location && location < child.EndLocation){
						if(pred == null || pred (child))
							result = child;

						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if(child == null)
					break;
			}
			return result;
		}
		
		/// <summary>
		/// Gets the node specified by T at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public T GetNodeAt<T>(int line, int column) where T : AstNode
		{
			return GetNodeAt<T>(new TextLocation(line, column));
		}
		
		/// <summary>
		/// Gets the node specified by T at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public T GetNodeAt<T>(TextLocation location) where T : AstNode
		{
			T result = null;
			AstNode node = this;
			while(node.FirstChild != null){
				var child = node.FirstChild;
				while(child != null){
					if(child.StartLocation <= location && location < child.EndLocation){
						if(child is T)
							result = (T)child;

						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if(child == null)
					break;
			}
			return result;
		}
		#endregion
		#endregion

		#region Node factories
		internal static Identifier MakeIdent(string name, TextLocation start, TextLocation end)
		{
			return new Identifier(name, start, end);
		}

		internal static IndexerExpression MakeIndexExpr(Expression target, Identifier key, TextLocation start, TextLocation end)
		{
			var res = new IndexerExpression(target, key, start, end);
			res.AddChild(target);
			res.AddChild(key);
			return res;
		}

		internal static InvocationExpression MakeInvoke(Expression invokeTarget, List<Expression> args, TextLocation start,
		                                                TextLocation end)
		{
			var res = new InvocationExpression(invokeTarget, args.ToArray(), start, end);
			res.AddChild(invokeTarget);
			foreach(var arg in res.Arguments)
				res.AddChild(arg);

			return res;
		}

		internal static LiteralExpression MakeLiteral(object value, TextLocation start, TextLocation end)
		{
			return new LiteralExpression(value, start, end);
		}

		internal static MemberReferenceExpression MakeMemRef(Expression target, Identifier reference, TextLocation start, TextLocation end)
		{
			var res = new MemberReferenceExpression(target, reference, start, end);
			res.AddChild(target);
			res.AddChild(reference);
			return res;
		}

		internal static Statement MakeStatement(Expression expr, TextLocation start, TextLocation end)
		{
			var res = new Statement(expr, start, end);
			res.AddChild(expr);
			return res;
		}

		internal static SyntaxTree MakeSyntaxTree(List<Statement> body, string name, TextLocation start, TextLocation end)
		{
			var res = new SyntaxTree(body.ToArray(), name, start, end);
			foreach(var stmt in res.Body)
				res.AddChild(stmt);

			return res;
		}

		internal static TimeFormatLiteral MakeTimeFormat(int hour, int min, int sec, TextLocation start, TextLocation end)
		{
			return new TimeFormatLiteral(hour, min, sec, start, end);
		}
		#endregion
	}
}

