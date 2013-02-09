using System;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;

using BVE5Language.Ast;

namespace BVE5Language.TypeSystem
{
	/// <summary>
	/// Produces type and member definitions from the DOM.
	/// </summary>
	public class TypeSystemConvertWalker : IAstWalker<IUnresolvedEntity>
	{
		readonly BVE5UnresolvedFile unresolved_file;
		
		/// <summary>
		/// Creates a new TypeSystemConvertVisitor.
		/// </summary>
		/// <param name="fileName">The file name (used for DomRegions).</param>
		public TypeSystemConvertWalker(string fileName)
		{
			if(fileName == null)
				throw new ArgumentNullException("fileName");

			this.unresolved_file = new BVE5UnresolvedFile(fileName);
		}
		
		/// <summary>
		/// Creates a new TypeSystemConvertVisitor and initializes it with a given context.
		/// </summary>
		/// <param name="unresolvedFile">The parsed file to which members should be added.</param>
		/// <param name="currentUsingScope">The current using scope.</param>
		/// <param name="currentTypeDefinition">The current type definition.</param>
		public TypeSystemConvertWalker(BVE5UnresolvedFile unresolvedFile)
		{
			if(unresolvedFile == null)
				throw new ArgumentNullException("unresolvedFile");

			this.unresolved_file = unresolvedFile;
		}
		
		public BVE5UnresolvedFile UnresolvedFile {
			get { return unresolved_file; }
		}
		
		DomRegion MakeRegion(TextLocation start, TextLocation end)
		{
			return new DomRegion(unresolved_file.FileName, start.Line, start.Column, end.Line, end.Column);
		}
		
		DomRegion MakeRegion(AstNode node)
		{
			if(node == null)
				return DomRegion.Empty;
			else
				return MakeRegion(node.StartLocation, node.EndLocation);
		}
		
		#region IAstWalker implementation
		public IUnresolvedEntity Walk(SyntaxTree unit)
		{
			unresolved_file.Errors = unit.Errors;
			return null;
		}

		public IUnresolvedEntity Walk(Identifier node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(IndexerExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(InvocationExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(LiteralExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(MemberReferenceExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(Statement node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(TimeFormatLiteral node)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

