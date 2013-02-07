//
// FindReferences.cs
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
using System.Threading;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;

using BVE5Language.TypeSystem;

using BVE5Language.Ast;

namespace MonoDevelop.BVEBinding.Resolver
{
	public delegate void FoundReferenceCallback(AstNode astNode, ResolveResult result);

	/// <summary>
	/// 'Find references' implementation.
	/// </summary>
	/// <remarks>
	/// This class is thread-safe.
	/// The intended multi-threaded usage is to call GetSearchScopes() once, and then
	/// call FindReferencesInFile() concurrently on multiple threads (parallel foreach over all interesting files).
	/// </remarks>
	/*public sealed class FindReferences
	{
		#region Properties
		/// <summary>
		/// Specifies whether find references should only look for specialized matches
		/// with equal type parameter substitution to the member we are searching for.
		/// </summary>
		public bool FindOnlySpecializedReferences { get; set; }
		#endregion
		
		#region class SearchScope
		sealed class SearchScope : IFindReferenceSearchScope
		{
			readonly Func<ICompilation, FindReferenceNavigator> factory;
			
			public SearchScope(Func<ICompilation, FindReferenceNavigator> factory)
			{
				this.factory = factory;
			}
			
			public SearchScope(string searchTerm, Func<ICompilation, FindReferenceNavigator> factory)
			{
				this.searchTerm = searchTerm;
				this.factory = factory;
			}
			
			internal string searchTerm;
			internal FindReferences findReferences;
			internal ICompilation declarationCompilation;
			internal ITypeDefinition topLevelTypeDefinition;
			
			IResolveVisitorNavigator IFindReferenceSearchScope.GetNavigator(ICompilation compilation, FoundReferenceCallback callback)
			{
				FindReferenceNavigator n = factory(compilation);
				if(n != null){
					n.callback = callback;
					n.findReferences = findReferences;
					return n;
				}else{
					return new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Skip, null);
				}
			}
			
			ICompilation IFindReferenceSearchScope.Compilation {
				get { return declarationCompilation; }
			}
			
			string IFindReferenceSearchScope.SearchTerm {
				get { return searchTerm; }
			}
			
			ITypeDefinition IFindReferenceSearchScope.TopLevelTypeDefinition {
				get { return topLevelTypeDefinition; }
			}
		}
		
		abstract class FindReferenceNavigator : IResolveVisitorNavigator
		{
			internal FoundReferenceCallback callback;
			internal FindReferences findReferences;
			
			internal abstract bool CanMatch(AstNode node);
			internal abstract bool IsMatch(ResolveResult rr);
			
			ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
			{
				if (CanMatch(node))
					return ResolveVisitorNavigationMode.Resolve;
				else
					return ResolveVisitorNavigationMode.Scan;
			}
			
			void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
			{
				if(CanMatch(node) && IsMatch(result))
					ReportMatch(node, result);
			}
			
			protected void ReportMatch(AstNode node, ResolveResult result)
			{
				if(callback != null)
					callback(node, result);
			}
			
			internal virtual void NavigatorDone(CSharpAstResolver resolver, CancellationToken cancellationToken)
			{
			}
		}
		#endregion
		
		#region GetSearchScopes
		public IList<IFindReferenceSearchScope> GetSearchScopes(IEntity entity)
		{
			if(entity == null)
				throw new ArgumentNullException("entity");

			if(entity is IMember)
				entity = NormalizeMember((IMember)entity);

			ITypeDefinition toplevel_type_def = entity.DeclaringTypeDefinition;
			while(toplevel_type_def != null && toplevel_type_def.DeclaringTypeDefinition != null)
				toplevel_type_def = toplevel_type_def.DeclaringTypeDefinition;

			SearchScope scope;
			SearchScope additionalScope = null;
			switch(entity.EntityType){
			case EntityType.TypeDefinition:
				scope = FindTypeDefinitionReferences((ITypeDefinition)entity, out additionalScope);
				break;

			case EntityType.Field:
				if(entity.DeclaringTypeDefinition != null && entity.DeclaringTypeDefinition.Kind == TypeKind.Enum)
					scope = FindMemberReferences(entity, m => new FindEnumMemberReferences((IField)m));
				else
					scope = FindMemberReferences(entity, m => new FindFieldReferences((IField)m));
				break;
			
			case EntityType.Indexer:
				scope = FindIndexerReferences((IProperty)entity);
				break;
			
			default:
				throw new ArgumentException("Unknown entity type " + entity.EntityType);
			}

			scope.declarationCompilation = entity.Compilation;
			scope.topLevelTypeDefinition = toplevel_type_def;
			scope.findReferences = this;
			if(additionalScope != null){
				additionalScope.declarationCompilation = entity.Compilation;
				additionalScope.topLevelTypeDefinition = toplevel_type_def;
				additionalScope.findReferences = this;
				return new[]{scope, additionalScope};
			}else{
				return new[]{scope};
			}
		}
		#endregion

		#region FindReferencesInFile
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScope">The search scope for which to look.</param>
		/// <param name="unresolvedFile">The type system representation of the file being searched.</param>
		/// <param name="syntaxTree">The syntax tree of the file being searched.</param>
		/// <param name="compilation">The compilation for the project that contains the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IFindReferenceSearchScope searchScope, BVE5UnresolvedFile unresolvedFile, SyntaxTree syntaxTree,
		                                 ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if(searchScope == null)
				throw new ArgumentNullException("searchScope");

			FindReferencesInFile(new[]{searchScope}, unresolvedFile, compilation, callback, cancellationToken);
		}
		
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScopes">The search scopes for which to look.</param>
		/// <param name="unresolvedFile">The type system representation of the file being searched.</param>
		/// <param name="compilation">The compilation for the project that contains the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IList<IFindReferenceSearchScope> searchScopes, BVE5UnresolvedFile unresolvedFile, SyntaxTree syntaxTree,
		                                 ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if(searchScopes == null)
				throw new ArgumentNullException("searchScopes");

			if(unresolvedFile == null)
				throw new ArgumentNullException("unresolvedFile");

			if(syntaxTree == null)
				throw new ArgumentNullException("syntaxTree");

			if(compilation == null)
				throw new ArgumentNullException("compilation");

			if(callback == null)
				throw new ArgumentNullException("callback");
			
			if(searchScopes.Count == 0)
				return;

			var navigators = new IResolveVisitorNavigator[searchScopes.Count];
			for(int i = 0; i < navigators.Length; i++)
				navigators[i] = searchScopes[i].GetNavigator(compilation, callback);

			IResolveVisitorNavigator combined_navigator;
			if(searchScopes.Count == 1)
				combined_navigator = navigators[0];
			else
				combined_navigator = new CompositeResolveVisitorNavigator(navigators);
			
			cancellationToken.ThrowIfCancellationRequested();
			combined_navigator = new DetectSkippableNodesNavigator(combined_navigator, syntaxTree);
			cancellationToken.ThrowIfCancellationRequested();
			CSharpAstResolver resolver = new CSharpAstResolver(compilation, syntaxTree, unresolvedFile);
			resolver.ApplyNavigator(combined_navigator, cancellationToken);
			foreach (var n in navigators) {
				var frn = n as FindReferenceNavigator;
				if(frn != null)
					frn.NavigatorDone(resolver, cancellationToken);
			}
		}
		#endregion
		
		#region Find TypeDefinition References
		SearchScope FindTypeDefinitionReferences(ITypeDefinition typeDefinition, bool findTypeReferencesEvenIfAliased,
		                                         out SearchScope additionalScope)
		{
			string searchTerm = null;
			additionalScope = null;
			if(!findTypeReferencesEvenIfAliased && KnownTypeReference.GetCSharpNameByTypeCode(typeDefinition.KnownTypeCode) == null){
				// We can optimize the search by looking only for the type references with the right identifier,
				// but only if it's not a primitive type and we're not looking for indirect references (through an alias)
				searchTerm = typeDefinition.Name;
			}
			return FindTypeDefinitionReferences(typeDefinition, searchTerm);
		}
		
		SearchScope FindTypeDefinitionReferences(ITypeDefinition typeDefinition, string searchTerm)
		{
			return new SearchScope(
				searchTerm,
				delegate (ICompilation compilation) {
				ITypeDefinition imported = compilation.Import(typeDefinition);
				if(imported != null)
					return new FindTypeDefinitionReferencesNavigator(imported, searchTerm);
				else
					return null;
			});
		}
		
		sealed class FindTypeDefinitionReferencesNavigator : FindReferenceNavigator
		{
			readonly ITypeDefinition typeDefinition;
			readonly string searchTerm;
			
			public FindTypeDefinitionReferencesNavigator(ITypeDefinition typeDefinition, string searchTerm)
			{
				this.typeDefinition = typeDefinition;
				this.searchTerm = searchTerm;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				IdentifierExpression ident = node as IdentifierExpression;
				if (ident != null)
					return searchTerm == null || ident.Identifier == searchTerm;
				
				MemberReferenceExpression mre = node as MemberReferenceExpression;
				if (mre != null)
					return searchTerm == null || mre.MemberName == searchTerm;
				
				SimpleType st = node as SimpleType;
				if (st != null)
					return searchTerm == null || st.Identifier == searchTerm;
				
				MemberType mt = node as MemberType;
				if (mt != null)
					return searchTerm == null || mt.MemberName == searchTerm;
				
				if (searchTerm == null && node is PrimitiveType)
					return true;
				
				return false;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				TypeResolveResult trr = rr as TypeResolveResult;
				return trr != null && typeDefinition.Equals(trr.Type.GetDefinition());
			}
		}
		#endregion
	}*/
}

