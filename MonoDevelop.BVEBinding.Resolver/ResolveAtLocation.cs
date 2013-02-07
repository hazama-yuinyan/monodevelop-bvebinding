//
// ResolveAtLocation.cs
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
using System.Threading;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

using BVE5Language.Ast;
using BVE5Language.Resolver;
using BVE5Language.TypeSystem;

namespace MonoDevelop.BVEBinding.Resolver
{
	public static class ResolveAtLocation
	{
		public static ResolveResult Resolve(Lazy<ICompilation> compilation, BVE5UnresolvedFile unresolvedFile, SyntaxTree syntaxTree, TextLocation location, out AstNode node,
		                                    CancellationToken cancellationToken = default(CancellationToken))
		{
			node = syntaxTree.GetNodeAt(location);
			if(node == null)
				return null;

			if(BVE5AstResolver.IsUnresolvableNode(node)){
				if(node is Identifier){
					node = node.Parent;
				}else if (node.NodeType == NodeType.Token){
					if(node.Parent is IndexerExpression){
						Console.WriteLine (2);
						// There's no other place where one could hover to see the indexer's tooltip,
						// so we need to resolve it when hovering over the '[' or ']'.
						// For constructor initializer, the same applies to the 'base'/'this' token.
						node = node.Parent;
					}else{
						return null;
					}
				}else{
					// don't resolve arbitrary nodes - we don't want to show tooltips for everything
					return null;
				}
			}else{
				// It's a resolvable node.
				// However, we usually don't want to show the tooltip everywhere
				// For example, hovering with the mouse over an empty line between two methods causes
				// node==TypeDeclaration, but we don't want to show any tooltip.
				
				if(!node.GetChildByRole(Roles.Identifier).IsNull){
					// We'll suppress the tooltip for resolvable nodes if there is an identifier that
					// could be hovered over instead:
					return null;
				}
			}
			if(node == null)
				return null;
			
			InvocationExpression parentInvocation = null;
			if((node is IdentifierExpression || node is MemberReferenceExpression) && node.Role != Roles.Argument){
				// we also need to resolve the invocation
				parentInvocation = node.Parent as InvocationExpression;
			}
			
			// TODO: I think we should provide an overload so that an existing CSharpAstResolver can be reused
			var resolver = new BVE5AstResolver(compilation.Value, syntaxTree, unresolvedFile);
			ResolveResult rr = resolver.Resolve(node, cancellationToken);
			if(rr is MethodGroupResolveResult && parentInvocation != null)
				return resolver.Resolve(parentInvocation);
			else
				return rr;
		}
	}
}

