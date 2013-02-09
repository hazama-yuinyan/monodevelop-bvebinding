//
// BVE5RouteFileParser.cs
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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Mono.CSharp;

using BVE5Language.Ast;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;


/**
 * BVE5のルートファイルには正式な仕様書のようなものはないが、コードコンプリーションなどの各種機能を実装するためにここに便宜的に仕様を決めることにする。
 * まず、BVE5のルートファイルの書式をEBNFで記すと概ね以下のようになるはずである。
 * 		route-file = meta-header, {header} , {body} ;
 * 		meta-header = "BveTs Map 1.00" ;
 * 		header = type , "." , method-name , "(" , {arguments} , ")" , ";" ;
 * 		body = digit , {digit} , ";" , ["\n"] , {content} ;
 * 		content = type , ["[" , name , "]"] , "." , method-name , "(" , {arguments} , ")" , ";" ;
 * 		arguments = name | number | time-literal | file-path ;
 * 		type = name ;
 * 		method-name = name ;
 * 		number = digit , {digit} [, "." , digit , {digit}] ;
 * 		name = {any character} ;
 * 		time-literal = number ":" number ":" number ;
 * 		file-path = {any character delimited by "\"} ;
 * 		digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;
 * BVE5のルートファイルには以下のような型のオブジェクトが出現する。
 * 		Primitive types : int, double, name
 * 		Complex types : TimeFormat, FilePath, enum<Tilt>, enum<Direction>, enum<ForwardingDirection>
 */


namespace BVE5Language.Parser
{
	/// <summary>
	/// BVE5 route file parser.
	/// </summary>
	public class BVE5RouteFileParser
	{
		internal static object parse_lock = new object();

		public BVE5RouteFileParser()
		{
			Console.WriteLine("Called ctor of BVE5RouteFileParser");
		}

		/*public class ErrorReportPrinter : ReportPrinter
		{
			readonly string fileName;
			public readonly List<Error> Errors = new List<Error>();
			
			public ErrorReportPrinter(string fileName)
			{
				this.fileName = fileName;
			}
			
			public override void Print(AbstractMessage msg)
			{
				base.Print(msg);
				var newError = new Error(msg.IsWarning ? ErrorType.Warning : ErrorType.Error, msg.Text, new DomRegion(fileName, msg.Location.Row, msg.Location.Column));
				Errors.Add(newError);
			}
		}
		ErrorReportPrinter error_report_printer = new ErrorReportPrinter(null);

		public bool HasErrors {
			get {
				return error_report_printer.ErrorsCount > 0;
			}
		}
		
		public bool HasWarnings {
			get {
				return error_report_printer.WarningsCount > 0;
			}
		}
		
		public IEnumerable<Error> Errors {
			get {
				return error_report_printer.Errors.Where(e => e.ErrorType == ErrorType.Error);
			}
		}
		
		public IEnumerable<Error> Warnings {
			get {
				return error_report_printer.Errors.Where(e => e.ErrorType == ErrorType.Warning);
			}
		}
		
		public IEnumerable<Error> ErrorsAndWarnings {
			get { return error_report_printer.Errors; }
		}*/

		#region public surface
		/// <summary>
		/// Parses the specified file at filePath.
		/// </summary>
		/// <param name='filePath'>
		/// Path to the file being parsed.
		/// </param>
		public SyntaxTree Parse(string filePath)
		{
			return ParseImpl(File.ReadAllText(filePath).Replace(Environment.NewLine, "\n"), filePath, true);
		}

		/// <summary>
		/// Parses a string.
		/// </summary>
		/// <param name='programSrc'>
		/// Program source in string format.
		/// </param>
		/// <param name='fileName'>
		/// File name. This is used for the SyntaxTree node.
		/// </param>
		public SyntaxTree Parse(string programSrc, string fileName = "")
		{
			return ParseImpl(programSrc.Replace(Environment.NewLine, "\n"), fileName, true);
		}

		/// <summary>
		/// Parses a route file comsuming the given stream as the program code source.
		/// </summary>
		/// <param name='stream'>
		/// Stream.
		/// </param>
		/// <param name='fileName'>
		/// File name. This is used for the SyntaxTree node.
		/// </param>
		public SyntaxTree Parse(Stream stream, string fileName = "")
		{
			return ParseImpl(new StreamReader(stream).ReadToEnd().Replace(Environment.NewLine, "\n"), fileName, true);
		}

		/// <summary>
		/// Parses a statement. This is especially intended for real-time parsing such as those occur during
		/// real-time analysis for text editor support.
		/// </summary>
		/// <param name='src'>
		/// Source string.
		/// </param>
		public BVE5Language.Ast.Statement ParseOneStatement(string src)
		{
			return ParseImpl(src/*.Replace(Environment.NewLine, "\n")*/, "<string>", false).Body[0];
		}
		#endregion

		#region Implemetation details
		private SyntaxTree ParseImpl(string src, string fileName, bool parseHeader)
		{
			lock(parse_lock){
				using(var reader = new StringReader(src)){
					var lexer = new BVE5RouteFileLexer(reader);
					if(parseHeader){
						int cur_line = lexer.CurrentLine;
						var tokens = new List<Token>();
						while(lexer.Current != Token.EOF && lexer.CurrentLine == cur_line){
							lexer.Advance();
							tokens.Add(lexer.Current);
						}
						if(tokens.Count != 3 || tokens[0].Literal != "BveTs" || tokens[1].Literal != "Map" ||
						   tokens[2].Literal != "1.00")
							throw new BVE5ParserException(1, 1, "Invalid Map file!");
					}else{
						lexer.Advance();
					}

					BVE5Language.Ast.Statement stmt = null;
					var stmts = new List<BVE5Language.Ast.Statement>();
					while(lexer.Current != Token.EOF){
						stmt = ParseStatement(lexer);
						stmts.Add(stmt);
					}

					return AstNode.MakeSyntaxTree(stmts, fileName, new TextLocation(1, 1), stmt.EndLocation);
				}
			}
		}

		// [\d]+ ';' | ident ['[' ident ']'] '.' ident '(' [args] ')' ';'
		private BVE5Language.Ast.Statement ParseStatement(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			BVE5Language.Ast.Expression expr = null;

			if(token.Kind != TokenKind.Comment){	//See if the line 
				if(token.Kind == TokenKind.IntegerLiteral){
					expr = ParseLiteral(lexer);
				}else if(token.Kind == TokenKind.Identifier){
					AstNode res = ParseIdent(lexer);
					if(lexer.Current.Literal == "["){
						res = ParseIndexExpr(lexer, res as BVE5Language.Ast.Expression);
					}

					token = lexer.Current;
					if(token.Literal != ".")
						throw new BVE5ParserException(token.Line, token.Column, "Expected '.' but got " + token.Literal);

					res = ParseMemberRef(lexer, res as BVE5Language.Ast.Expression);
					expr = ParseInvokeExpr(lexer, res as BVE5Language.Ast.Expression);
				}else{
					throw new BVE5ParserException(token.Line, token.Column,
					                              "A statement must start with an integer literal or an identifier.");
				}

				token = lexer.Current;
				if(token.Literal != ";")
					throw new BVE5ParserException(token.Line, token.Column, "Unexpected character: " + token.Literal);

				lexer.Advance();
			}

			token = lexer.Current;
			return AstNode.MakeStatement(expr, expr.StartLocation, token.StartLoc);
		}

		// any-character-except-(-)-.-;-[-]
		private Identifier ParseIdent(BVE5RouteFileLexer lexer)
		{
			Debug.Assert(lexer.Current.Kind == TokenKind.Identifier, "Really meant an identifier?");
			Token token = lexer.Current;
			lexer.Advance();
			return AstNode.MakeIdent(token.Literal, token.StartLoc, lexer.Current.StartLoc);
		}

		// ident '[' ident ']'
		private IndexerExpression ParseIndexExpr(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression target)
		{
			Debug.Assert(lexer.Current.Literal == "[", "Really meant an index reference?");
			lexer.Advance();
			Token token = lexer.Current;
			if(token.Kind != TokenKind.Identifier)
				throw new BVE5ParserException(token.Line, token.Column, "Unexpected token: " + token.Kind);

			Identifier ident = ParseIdent(lexer);
			token = lexer.Current;
			if(token.Literal != "]")
				throw new BVE5ParserException(token.Line, token.Column, "Expected ']' but got " + token.Literal);

			var end_loc = token.EndLoc;
			lexer.Advance();
			return AstNode.MakeIndexExpr(target, ident, target.StartLocation, end_loc);
		}

		// ident '.' ident
		private MemberReferenceExpression ParseMemberRef(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression parent)
		{
			Debug.Assert(lexer.Current.Literal == ".", "Really meant a member reference?");
			lexer.Advance();
			Token token = lexer.Current;
			if(token.Kind != TokenKind.Identifier)
				throw new BVE5ParserException(token.Line, token.Column, "Unexpected token: " + token.Kind);

			Identifier ident = ParseIdent(lexer);
			return AstNode.MakeMemRef(parent, ident, parent.StartLocation, ident.EndLocation);
		}

		// expr '(' [arguments] ')'
		private InvocationExpression ParseInvokeExpr(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression callTarget)
		{
			Debug.Assert(lexer.Current.Literal == "(", "Really meant an invoke expression?");
			lexer.Advance();
			Token token = lexer.Current;
			var args = new List<BVE5Language.Ast.Expression>();

			while(token.Kind != TokenKind.EOF && token.Literal != ")"){
				if(token.Kind == TokenKind.Identifier){
					args.Add(ParseIdent(lexer));
				}else if(token.Kind == TokenKind.IntegerLiteral){
					var la = lexer.Peek;
					if(la.Literal == ":")
						args.Add(ParseTimeLiteral(lexer));
					else
						args.Add(ParseLiteral(lexer));
				}else{
					throw new BVE5ParserException(token.Line, token.Column,
					                              "An argument must be an identifier or a time format literal!");
				}

				token = lexer.Current;
				if(token.Literal == ","){
					lexer.Advance();
					token = lexer.Current;
				}
			}

			if(token.Kind == TokenKind.EOF)
				throw new BVE5ParserException(token.Line, token.Column, "Unexpected EOF!");

			return AstNode.MakeInvoke(callTarget, args, callTarget.StartLocation, token.EndLoc);
		}

		// number
		private LiteralExpression ParseLiteral(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral || token.Kind == TokenKind.FloatLiteral, "Really meant a literal?");
			lexer.Advance();
			return AstNode.MakeLiteral(token.Kind == TokenKind.FloatLiteral ? Convert.ToDouble(token.Literal) : Convert.ToInt32(token.Literal),
			                           token.StartLoc, token.EndLoc);
		}

		// number ':' number ':' number
		private TimeFormatLiteral ParseTimeLiteral(BVE5RouteFileLexer lexer)
		{
			Debug.Assert(lexer.Current.Kind == TokenKind.IntegerLiteral, "Really meant a timel literal?");
			int[] nums = new int[3];
			Token token;
			Token start_token = token = lexer.Current;

			for(int i = 0; i < 3; ++i){
				if(token.Kind == TokenKind.EOF)
					throw new BVE5ParserException(token.Line, token.Column, "Unexpected EOF!");

				nums[i] = Convert.ToInt32(token.Literal);
				if(i == 2) break;

				lexer.Advance();
				token = lexer.Current;
				if(token.Kind == TokenKind.EOF)
					throw new BVE5ParserException(token.Line, token.Column, "Unexpected EOF!");
				else if(token.Literal != ":")
					throw new BVE5ParserException(token.Line, token.Column, "Expected ':' but got " + token.Literal);

				lexer.Advance();
				token = lexer.Current;
			}

			return AstNode.MakeTimeFormat(nums[0], nums[1], nums[2], start_token.StartLoc, token.StartLoc);
		}
		#endregion
	}
}

