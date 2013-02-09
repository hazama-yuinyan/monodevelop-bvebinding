using System;

using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a statement.
	/// </summary>
	public class Statement : AstNode
	{
		private readonly Expression expr;

		public Expression Expr{
			get{return expr;}
		}

		public override NodeType Type {
			get {
				return NodeType.Statement;
			}
		}

		public Statement(Expression inputExpr, TextLocation startLoc, TextLocation endLoc)
			: base(startLoc, endLoc)
		{
			expr = inputExpr;
		}

		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				if(expr != null)
					expr.AcceptWalker(walker);
			}

			walker.PostWalk(this);
		}

		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}

		public override string GetText()
		{
			return expr.GetText() + ";";
		}
	}
}

