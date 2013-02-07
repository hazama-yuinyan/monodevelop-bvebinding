//
// BVE5ReferenceFinder.cs
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
using System.Linq;
using System.Threading;

using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

using BVE5Language.Ast;
using BVE5Language.Parser;
using BVE5Language.TypeSystem;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;

using Mono.TextEditor;

namespace MonoDevelop.BVEBinding.Refactoring
{
	/*public class BVE5ReferenceFinder : ReferenceFinder
	{
		Resolver.FindReferences ref_finder = new Resolver.FindReferences();
		List<object> searched_members;
		List<FilePath> files = new List<FilePath>();
		List<Tuple<FilePath, MonoDevelop.Ide.Gui.Document>> open_docs = new List<Tuple<FilePath, MonoDevelop.Ide.Gui.Document>>();
		
		string member_name;
		string keyword_name;
		
		public BVE5ReferenceFinder()
		{
			IncludeDocumentation = true;
		}
		
		public void SetSearchedMembers(IEnumerable<object> members)
		{
			searched_members = new List<object>(members);
			var firstMember = searched_members.FirstOrDefault();

			if(firstMember is INamedElement){
				var named_elem = (INamedElement)firstMember;
				var name = named_elem.Name;

				member_name = name;
				
				//keyword_name = CSharpAmbience.NetToCSharpTypeName(named_elem.FullName);
				if(keyword_name == named_elem.FullName)
					keyword_name = null;
			}
			//if(firstMember is string)
			//	memberName = firstMember.ToString();

			//if(firstMember is IVariable)
			//	memberName = ((IVariable)firstMember).Name;

			//if(firstMember is ITypeParameter)
			//	memberName = ((ITypeParameter)firstMember).Name;
		}
		
		void SetPossibleFiles(IEnumerable<FilePath> files)
		{
			foreach(var file in files){
				var open_doc = IdeApp.Workbench.GetDocument(file);
				if(open_doc == null)
					this.files.Add(file);
				else
					this.open_docs.Add(Tuple.Create(file, open_doc));
			}
		}
		
		MemberReference GetReference(ResolveResult result, AstNode node, string fileName, Mono.TextEditor.TextEditorData editor)
		{
			if(result == null)
				return null;
			
			object valid = null;
			if(result is MethodGroupResolveResult){
				valid = ((MethodGroupResolveResult)result).Methods.FirstOrDefault(
					m => searched_members.Any(member => member is IMethod && ((IMethod)member).Region == m.Region));
			}else if(result is MemberResolveResult){
				var foundMember = ((MemberResolveResult)result).Member;
				valid = searched_members.FirstOrDefault(
					member => member is IMember && ((IMember)member).Region == foundMember.Region);
			}else if(result is TypeResolveResult){
				valid = searched_members.FirstOrDefault(n => n is IType);
			}else{
				valid = searched_members.FirstOrDefault();
			}
			
			if (node is InvocationExpression)
				node = ((InvocationExpression)node).Target;
			
			if (node is MemberReferenceExpression)
				node = ((MemberReferenceExpression)node).Reference;
			
			if (node is MemberType)
				node = ((MemberType)node).MemberNameToken;
			
			if (node is TypeDeclaration && (searched_members.First() is IType)) 
				node = ((TypeDeclaration)node).NameToken;
			
			if (node is EntityDeclaration && (searched_members.First() is IMember)) 
				node = ((EntityDeclaration)node).NameToken;
			
			if (node is ParameterDeclaration && (searched_members.First () is IParameter)) 
				node = ((ParameterDeclaration)node).NameToken;
			
			if (node is IdentifierExpression) {
				node = ((IdentifierExpression)node).IdentifierToken;
			}
			
			var region = new DomRegion(fileName, node.StartLocation, node.EndLocation);
			
			var length = node is PrimitiveType ? keyword_name.Length : node.EndLocation.Column - node.StartLocation.Column;
			return new MemberReference(valid, region, editor.LocationToOffset(region.Begin), length);
		}
		
		bool IsNodeValid(object searchedMember, AstNode node)
		{
			if(searchedMember is IField && node is FieldDeclaration)
				return false;

			return true;
		}
		
		public IEnumerable<MemberReference> FindInDocument(MonoDevelop.Ide.Gui.Document doc)
		{
			if(string.IsNullOrEmpty (member_name))
				return Enumerable.Empty<MemberReference>();

			var editor = doc.Editor;
			var parsed_doc = doc.ParsedDocument;

			if(parsed_doc == null)
				return Enumerable.Empty<MemberReference>();

			var file = parsed_doc.ParsedFile as BVE5UnresolvedFile;
			var result = new List<MemberReference>();
			
			foreach(var obj in searched_members){
				if(obj is IEntity){
					ref_finder.FindReferencesInFile(ref_finder.GetSearchScopes((IEntity)obj), file, unit, doc.Compilation, (astNode, r) => {
						if(IsNodeValid (obj, astNode))
							result.Add(GetReference (r, astNode, editor.FileName, editor)); 
					}, CancellationToken.None);
				}else if(obj is IVariable){
					ref_finder.FindLocalReferences((IVariable)obj, file, unit, doc.Compilation, (astNode, r) => { 
						if (IsNodeValid (obj, astNode))
							result.Add (GetReference (r, astNode, editor.FileName, editor));
					}, CancellationToken.None);
				} else if (obj is ITypeParameter) {
					ref_finder.FindTypeParameterReferences ((ITypeParameter)obj, file, unit, doc.Compilation, (astNode, r) => { 
						if (IsNodeValid (obj, astNode))
							result.Add (GetReference (r, astNode, editor.FileName, editor));
					}, CancellationToken.None);
				}
			}
			return result;
		}
		
		public override IEnumerable<MemberReference> FindReferences(Project project, IProjectContent content,
		                                                            IEnumerable<FilePath> possibleFiles, IEnumerable<object> members)
		{
			if(content == null)
				throw new ArgumentNullException ("content", "Project content not set.");

			SetPossibleFiles(possibleFiles);
			SetSearchedMembers(members);
			
			var scopes = searched_members.Select(e => ref_finder.GetSearchScopes(e as IEntity));
			var compilation = project != null ? TypeSystemService.GetCompilation(project) : content.CreateCompilation();
			List<MemberReference> refs = new List<MemberReference>();

			foreach(var opendoc in open_docs){
				foreach(var new_ref in FindInDocument(opendoc.Item2)){
					if(new_ref == null || refs.Any (r => r.FileName == new_ref.FileName && r.Region == new_ref.Region))
						continue;

					refs.Add(new_ref);
				}
			}
			
			foreach(var file in files){
				string text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText(file);
				if(member_name != null && text.IndexOf(member_name, StringComparison.Ordinal) < 0 &&
				    (keyword_name == null || text.IndexOf(keyword_name, StringComparison.Ordinal) < 0))
					continue;

				using(var editor = TextEditorData.CreateImmutable(text)){
					editor.Document.FileName = file;
					var unresolved_file = new BVE5RouteFileParser().Parse(editor);
					if(unresolved_file == null)
						continue;
					
					var stored_file = content.GetFile(file);
					var parsed_file = stored_file as BVE5UnresolvedFile;
					
					if(parsed_file == null && stored_file is ParsedDocumentDecorator)
						parsed_file = ((ParsedDocumentDecorator)stored_file).ParsedFile as BVE5UnresolvedFile;
					
					if(parsed_file == null){
						// for fallback purposes - should never happen.
						parsed_file = unresolved_file.ToTypeSystem();
						content = content.AddOrUpdateFiles(content.GetFile(file), parsed_file);
						compilation = content.CreateCompilation();
					}

					foreach(var scope in scopes){
						ref_finder.FindReferencesInFile(
							scope,
							parsed_file,
							unresolved_file,
							compilation,
							(astNode, result) => {
							var new_ref = GetReference(result, astNode, file, editor);
							if(new_ref == null || refs.Any(r => r.FileName == new_ref.FileName && r.Region == new_ref.Region))
								return;

							refs.Add(new_ref);
						},
						CancellationToken.None
						);
					}
				}
			}
			return refs;
		}
	}*/
}

