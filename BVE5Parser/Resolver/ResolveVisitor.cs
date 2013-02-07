using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using ICSharpCode.NRefactory.Semantics;

using BVE5Language.Ast;
using BVE5Language.TypeSystem;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// Traverses the DOM and resolves expressions.
	/// </summary>
	/// <remarks>
	/// The ResolveVisitor does two jobs at the same time: it tracks the resolve context (properties on CSharpResolver)
	/// and it resolves the expressions visited.
	/// To allow using the context tracking without having to resolve every expression in the file (e.g. when you want to resolve
	/// only a single node deep within the DOM), you can use the <see cref="IResolveVisitorNavigator"/> interface.
	/// The navigator allows you to switch the between scanning mode and resolving mode.
	/// In scanning mode, the context is tracked (local variables registered etc.), but nodes are not resolved.
	/// While scanning, the navigator will get asked about every node that the resolve visitor is about to enter.
	/// This allows the navigator whether to keep scanning, whether switch to resolving mode, or whether to completely skip the
	/// subtree rooted at that node.
	/// 
	/// In resolving mode, the context is tracked and nodes will be resolved.
	/// The resolve visitor may decide that it needs to resolve other nodes as well in order to resolve the current node.
	/// In this case, those nodes will be resolved automatically, without asking the navigator interface.
	/// For child nodes that are not essential to resolving, the resolve visitor will switch back to scanning mode (and thus will
	/// ask the navigator for further instructions).
	/// 
	/// Moreover, there is the <c>ResolveAll</c> mode - it works similar to resolving mode, but will not switch back to scanning mode.
	/// The whole subtree will be resolved without notifying the navigator.
	/// </remarks>
	internal sealed class ResolveVisitor : IAstWalker<ResolveResult>
	{
		static private readonly ResolveResult errorResult = ErrorResolveResult.UnknownError;
		
		private BVE5Resolver resolver;
		private readonly BVE5UnresolvedFile unresolved_file;
		private readonly Dictionary<AstNode, ResolveResult> resolveResultCache = new Dictionary<AstNode, ResolveResult>();
		private readonly Dictionary<AstNode, BVE5Resolver> resolverBeforeDict = new Dictionary<AstNode, BVE5Resolver>();
		private readonly Dictionary<AstNode, BVE5Resolver> resolverAfterDict = new Dictionary<AstNode, BVE5Resolver>();
		
		//IResolveVisitorNavigator navigator;
		bool resolver_enabled;
		internal CancellationToken cancellation_token;
		
		#region Constructor
		/// <summary>
		/// Creates a new ResolveVisitor instance.
		/// </summary>
		public ResolveVisitor(BVE5Resolver inputResolver, BVE5UnresolvedFile unresolvedFile)
		{
			if(inputResolver == null)
				throw new ArgumentNullException("inputResolver");

			resolver = inputResolver;
			unresolved_file = unresolvedFile;
		}
		#endregion

		#region ResetContext
		/// <summary>
		/// Resets the visitor to the stored position, runs the action, and then reverts the visitor to the previous position.
		/// </summary>
		void ResetContext(BVE5Resolver storedContext, Action action)
		{
			var oldResolverEnabled = resolver_enabled;
			var oldResolver = this.resolver;
			try{
				resolver_enabled = false;
				this.resolver = storedContext;
				
				action();
			}finally{
				resolver_enabled = oldResolverEnabled;
				this.resolver = oldResolver;
			}
		}
		#endregion

		private void StoreCurrentState(AstNode node)
		{
			// It's possible that we re-visit an expression that we scanned over earlier,
			// so we might have to overwrite an existing state.
			
			resolverBeforeDict[node] = resolver;
		}
		
		private void StoreResult(AstNode node, ResolveResult result)
		{
			Debug.Assert(result != null);
			Log.WriteLine("Resolved '{0}' to {1}", node, result);
			Debug.Assert(!BVE5AstResolver.IsUnresolvableNode(node));
			// The state should be stored before the result is.
			Debug.Assert(resolverBeforeDict.ContainsKey(node));
			// Don't store results twice.
			Debug.Assert(!resolveResultCache.ContainsKey(node));
			resolveResultCache[node] = result;
		}
		
		#region Scan / Resolve
		/// <summary>
		/// Scans the AST rooted at the given node.
		/// </summary>
		public void Scan(AstNode node)
		{
			if(node == null)
				return;

			// don't Scan again if the node was already resolved
			if(resolveResultCache.ContainsKey(node)){
				// Restore state change caused by this node:
				BVE5Resolver new_resolver;
				if(resolverAfterDict.TryGetValue(node, out new_resolver))
					resolver = new_resolver;

				return;
			}
			
			Resolve(node);
		}
		
		/// <summary>
		/// Equivalent to 'Scan', but also resolves the node at the same time.
		/// This method should be only used if the CSharpResolver passed to the ResolveVisitor was manually set
		/// to the correct state.
		/// Otherwise, use <c>resolver.Scan(syntaxTree); var result = resolver.GetResolveResult(node);</c>
		/// instead.
		/// --
		/// This method now is internal, because it is difficult to use correctly.
		/// Users of the public API should use Scan()+GetResolveResult() instead.
		/// </summary>
		internal ResolveResult Resolve(AstNode node)
		{
			if(node == null)
				return errorResult;

			bool oldResolverEnabled = resolver_enabled;
			resolver_enabled = true;
			ResolveResult result;

			if(!resolveResultCache.TryGetValue(node, out result)){
				cancellation_token.ThrowIfCancellationRequested();
				StoreCurrentState(node);
				var oldResolver = resolver;
				result = node.AcceptWalker(this) ?? errorResult;
				StoreResult(node, result);
				if(resolver != oldResolver){
					// The node changed the resolver state:
					resolverAfterDict.Add(node, resolver);
				}
			}
			resolver_enabled = oldResolverEnabled;
			return result;
		}
		#endregion

		/// <summary>
		/// Gets the resolve result for the specified node.
		/// If the node was not resolved by the navigator, this method will resolve it.
		/// </summary>
		public ResolveResult GetResolveResult(AstNode node)
		{
			Debug.Assert(!BVE5AstResolver.IsUnresolvableNode(node));
			
			ResolveResult result;
			if(resolveResultCache.TryGetValue(node, out result))
				return result;
			
			AstNode parent;
			BVE5Resolver stored_resolver = GetPreviouslyScannedContext(node, out parent);
			ResetContext(
				stored_resolver,
				() => {
				Debug.Assert(!resolver_enabled);
				Scan(parent);
			});
			
			return resolveResultCache[node];
		}

		BVE5Resolver GetPreviouslyScannedContext(AstNode node, out AstNode parent)
		{
			parent = node;
			BVE5Resolver stored_resolver;
			while(!resolverBeforeDict.TryGetValue(parent, out stored_resolver)){
				parent = parent.Parent;
				if(parent == null)
					throw new InvalidOperationException("Could not find a resolver state for any parent of the specified node. Are you trying to resolve a node that is not a descendant of the CSharpAstResolver's root node?");
			}
			return stored_resolver;
		}

		#region AstWalker members
		public ResolveResult Walk(Identifer node)
		{
			return null;
		}

		public ResolveResult Walk(IndexerExpression node)
		{
			return null;
		}

		public ResolveResult Walk(InvocationExpression node)
		{
			return null;
		}

		public ResolveResult Walk(LiteralExpression node)
		{
			return null;
		}

		public ResolveResult Walk(MemberReferenceExpression node)
		{
			return null;
		}

		public ResolveResult Walk(Statement node)
		{
			return null;
		}

		public ResolveResult Walk(SyntaxTree node)
		{
			return null;
		}

		public ResolveResult Walk(TimeFormatLiteral node)
		{
			return null;
		}
		#endregion
	}
}

