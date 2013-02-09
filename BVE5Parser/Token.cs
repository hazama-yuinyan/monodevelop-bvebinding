using System;

using ICSharpCode.NRefactory;

namespace BVE5Language.Parser
{
	internal enum TokenKind
	{
		SyntaxToken,
		Identifier,
		IntegerLiteral,
		FloatLiteral,
		Comment,
		EOF
	}
	
	internal class Token
	{
		private TokenKind kind;
		private readonly int line, column;			//the line and column number where the token appears
		private readonly string token_literal;		//source string for this token
		internal static Token EOF = new Token(-1, -1, "<EOF>", TokenKind.EOF);
		
		public TextLocation StartLoc{
			get{return new TextLocation(line, column);}
		}
		
		public TextLocation EndLoc{
			get{return new TextLocation(line, column + token_literal.Length);}
		}
		
		public int Line{
			get{return line;}
		}
		
		public int Column{
			get{return column;}
		}
		
		public string Literal{
			get{return token_literal;}
		}
		
		public TokenKind Kind{
			get{return kind;}
		}
		
		public Token(int lineNum, int columnNum, string literal, TokenKind tokenKind)
		{
			line = lineNum;
			column = columnNum;
			token_literal = literal;
			kind = tokenKind;
		}
	}
}

