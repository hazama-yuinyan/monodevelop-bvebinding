using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;

using ICSharpCode.NRefactory;

namespace BVE5Language.Parser
{
	public partial class BVE5RouteFileParser
	{
		internal class Lexer
		{
			private readonly TextReader reader;
			private Token token = null,		//current token
				la = null;					//lookahead token
			private int line_num = 1, column_num = 1;
			private const char EOF = unchecked((char)-1);

			public Token Current{
				get{return token;}
			}

			public int CurrentLine{
				get{return token.Line;}
			}

			/// <summary>
			/// Peeks the next token without advancing .
			/// </summary>
			/// <value>
			/// The peeked token.
			/// </value>
			public Token Peek{
				get{return la;}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="BVE5Language.Parser.BVE5RouteFileParser+Lexer"/> class.
			/// </summary>
			/// <remarks>
			/// It assumes that the source string doesn't contain any \r.
			/// Otherwise it failes to tokenize.
			/// </remarks>				
			/// <param name='srcReader'>
			/// Source reader.
			/// </param>
			public Lexer(TextReader srcReader)
			{
				if(srcReader == null)
					throw new ArgumentNullException("srcReader");

				reader = srcReader;
				LookAheadToken();
			}

			/// <summary>
			/// Checks whether the lexer hits the EOF.
			/// </summary>
			/// <returns>
			/// <c>true</c>, if EOF was encountered, <c>false</c> otherwise.
			/// </returns>
			public bool HitEOF()
			{
				return token == Token.EOF;
			}

			/// <summary>
			/// Tells the lexer to read the next token and to store it on the internal buffer.
			/// </summary>
			public void Advance()
			{
				if(la != null){
					token = la;
				}else{
					token = Token.EOF;
					return;
				}

				LookAheadToken();
			}

			private void LookAheadToken()
			{
				SkipWhitespace();
				
				char ch = PeekChar();
				switch(ch){
				case EOF:
					la = null;
					break;
					
				case '(':
				case ')':
				case '.':
				case ';':
				case ':':
					GetChar();
					la = new Token(line_num, column_num, ch.ToString(), TokenKind.SyntaxToken);
					++column_num;
					break;
					
				case '-':
					la = GetIdOrNumber();
					break;
					
				case '0': case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8': case '9':
					la = GetNumber(true);
					break;
					
				default:
					la = GetId();
					break;
				}
			}

			private Token GetIdOrNumber()
			{
				char ch = GetChar();
				Debug.Assert(ch == EOF || ch != '-', "Attempted to parse an ID or number without hyphen.");
				ch = PeekChar();

				if(IsNumChar(ch))
					return GetNumber(false);
				else
					return GetId();
			}

			private Token GetNumber(bool isPositive)
			{
				char c = PeekChar();
				Debug.Assert(c == EOF || !IsNumChar(c), "Really meant a number?");

				var sb = new StringBuilder(isPositive ? "-" : "");
				c = PeekChar();
				bool found_dot = false;
				var start_column_num = column_num;

				while(c != EOF && IsNumChar(c) || c == '.'){
					if(c == '.'){
						if(found_dot)
							throw new BVE5ParserException(line_num, column_num, "A number literal can't have multiple dots in it!");

						found_dot = true;
					}
					sb.Append(c);
					GetChar();
					++column_num;
					c = PeekChar();
				}

				return new Token(line_num, start_column_num, sb.ToString(),
				                 found_dot ? TokenKind.FloatLiteral : TokenKind.IntegerLiteral);
			}

			// GetId has first param to handle call from GetIdOrNumber where
			// ID started with a hyphen (and looked like a number). Didn't want to
			// add hyphen to StartId test since it might appear as the keyword
			// minus in the future. Usually the overload without the first param is called.
			//
			// Must not call when the next char is EOF.
			//
			private Token GetId()
			{
				return GetId(GetChar());
			}

			private Token GetId(char first)
			{
				Debug.Assert(first != EOF && !IsIdTerminator(first), "Really meant an id?");
				var sb = new StringBuilder(first);
				// See if there's more chars to Id
				char ch = PeekChar();
				while(ch != EOF && (!IsIdTerminator(ch))){
					sb.Append(ch);
					GetChar();
					ch = PeekChar();
				}
				var tmp = new Token(line_num, column_num, sb.ToString(), TokenKind.Identifier);
				column_num += sb.Length;
				return tmp;
			}

			private static char[] id_terminators = new[]{'(', ')', '[', ']', ';', ',', '.'};

			private static bool IsIdTerminator(char c)
			{
				return id_terminators.Contains(c) || (c < (char)33);
			}

			private void SkipWhitespace()
			{
				char ch = PeekChar();
				while(char.IsWhiteSpace(ch) || ch == '/'){
					if(ch == '/'){	//skip to the next line since there is no other token in BVE5 Route file that starts with '/'
						do{
							GetChar();
							ch = PeekChar();
						}while(ch != EOF && ch != '\n');
						++line_num;
						column_num = 1;
					}else{
						if(ch == '\n'){
							++line_num;
							column_num = 1;
						}else{
							++column_num;
						}
						GetChar();
						ch = PeekChar();
					}
				}
			}

			private char GetChar()
			{
				return unchecked((char)reader.Read());
			}

			private char PeekChar()
			{
				return unchecked((char)reader.Peek());
			}

			private static bool IsNumChar(char c)
			{
				return '0' <= c && c <= '9';
			}
		}

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
}

