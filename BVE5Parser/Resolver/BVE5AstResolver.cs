using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Newtonsoft.Json;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

using BVE5Language.Ast;
using BVE5Language.TypeSystem;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// Resolves BVE5 AST nodes.
	/// </summary>
	/// <remarks>This class is thread-safe.</remarks>
	public class BVE5AstResolver
	{
		private readonly BVE5Resolver initial_resolver_state;
		private readonly AstNode root_node;
		private readonly BVE5UnresolvedFile unresolved_file;
		private readonly ResolveVisitor resolve_visitor;
		bool resolver_initialized;
		
		/// <summary>
		/// Creates a new BVE5 AST resolver.
		/// Use this overload if you are resolving within a complete BVE5 file.
		/// </summary>
		/// <param name="compilation">The current compilation.</param>
		/// <param name="syntaxTree">The syntax tree to be resolved.</param>
		/// <param name="unresolvedFile">
		/// Optional: Result of <see cref="SyntaxTree.ToTypeSystem()"/> for the file being resolved.
		/// <para>
		/// This is used for setting up the context on the resolver. The unresolved file must be registered in the compilation.
		/// </para>
		/// <para>
		/// When a unresolvedFile is specified, the resolver will use the member's StartLocation/EndLocation to identify
		/// member declarations in the AST with members in the type system.
		/// When no unresolvedFile is specified (<c>null</c> value for this parameter), the resolver will instead compare the
		/// member's signature in the AST with the signature in the type system.
		/// </para>
		/// </param>
		public BVE5AstResolver(ICompilation compilation, SyntaxTree syntaxTree, BVE5UnresolvedFile unresolvedFile = null)
		{
			if(compilation == null)
				throw new ArgumentNullException("compilation");

			if(syntaxTree == null)
				throw new ArgumentNullException("syntaxTree");

			initial_resolver_state = new BVE5Resolver(compilation);
			root_node = syntaxTree;
			unresolved_file = unresolvedFile;
			resolve_visitor = new ResolveVisitor(initial_resolver_state, unresolvedFile);
		}
		
		/// <summary>
		/// Creates a new BVE5 AST resolver.
		/// Use this overload if you are resolving code snippets (not necessarily complete files).
		/// </summary>
		/// <param name="resolver">The resolver state at the root node (to be more precise: just outside the root node).</param>
		/// <param name="rootNode">The root node of the tree to be resolved.</param>
		/// <param name="unresolvedFile">
		/// Optional: Result of <see cref="SyntaxTree.ToTypeSystem()"/> for the file being resolved.
		/// <para>
		/// This is used for setting up the context on the resolver. The unresolved file must be registered in the compilation.
		/// </para>
		/// <para>
		/// When a unresolvedFile is specified, the resolver will use the member's StartLocation/EndLocation to identify
		/// member declarations in the AST with members in the type system.
		/// When no unresolvedFile is specified (<c>null</c> value for this parameter), the resolver will instead compare the
		/// member's signature in the AST with the signature in the type system.
		/// </para>
		/// </param>
		public BVE5AstResolver(BVE5Resolver resolver, AstNode rootNode, BVE5UnresolvedFile unresolvedFile = null)
		{
			if(resolver == null)
				throw new ArgumentNullException("resolver");

			if(rootNode == null)
				throw new ArgumentNullException("rootNode");

			initial_resolver_state = resolver;
			root_node = rootNode;
			unresolved_file = unresolvedFile;
			resolve_visitor = new ResolveVisitor(initial_resolver_state, unresolvedFile);
		}
		
		/// <summary>
		/// Gets the type resolve context for the root resolver.
		/// </summary>
		public BVE5TypeResolveContext TypeResolveContext {
			get { return initial_resolver_state.CurrentTypeResolveContext; }
		}
		
		/// <summary>
		/// Gets the compilation for this resolver.
		/// </summary>
		public ICompilation Compilation {
			get { return initial_resolver_state.Compilation; }
		}
		
		/// <summary>
		/// Gets the root node for which this BVE5AstResolver was created.
		/// </summary>
		public AstNode RootNode {
			get { return root_node; }
		}
		
		/// <summary>
		/// Gets the unresolved file used by this BVE5AstResolver.
		/// Can return null.
		/// </summary>
		public BVE5UnresolvedFile UnresolvedFile {
			get { return unresolved_file; }
		}
		
		/// <summary>
		/// Resolves the specified node.
		/// </summary>
		public ResolveResult Resolve(AstNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			if(node == null)
				return ErrorResolveResult.UnknownError;

			lock(resolve_visitor){
				InitResolver();
				resolve_visitor.cancellation_token = cancellationToken;
				try{
					ResolveResult rr = resolve_visitor.GetResolveResult(node);
					if(rr == null){
						Debug.Fail(string.Format("{0} resolved to null.{1}:'{2}'", node.GetType(), node.StartLocation,
						                         node.GetText()));
					}

					return rr;
				}finally{
					resolve_visitor.cancellation_token = CancellationToken.None;
				}
			}
		}
		
		void InitResolver()
		{
			if(!resolver_initialized){
				resolver_initialized = true;
				resolve_visitor.Scan(root_node);
			}
		}

		public static bool IsUnresolvableNode(AstNode node)
		{
			return false;
		}
	}
}

