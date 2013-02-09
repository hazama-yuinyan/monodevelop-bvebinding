using System;

namespace BVE5Language.Ast
{
	public enum NodeType
	{
		None,
		Identifier,
		Indexer,
		Invocation,
		Literal,
		MemRef,
		Statement,
		Tree,
		TimeLiteral
	}
}

