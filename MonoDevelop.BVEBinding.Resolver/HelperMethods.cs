//
// HelperMethods.cs
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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

using Mono.TextEditor;

using BVE5Language.TypeSystem;

using BVE5Language.Ast;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.BVEBinding.Resolver
{
	public static class HelperMethods
	{
		public static SyntaxTree Parse(this BVE5Language.Parser.BVE5RouteFileParser parser, TextEditorData editorData)
		{
			using(var stream = editorData.OpenStream()){
				return parser.Parse(stream, editorData.Document.FileName);
			}
		}

		public static bool TryResolveAt(this Document doc, DocumentLocation loc, out ResolveResult result, out BVE5UnresolvedFile file)
		{
			if(doc == null)
				throw new ArgumentNullException("doc");

			result = null;
			file = null;
			var parsed_doc = doc.ParsedDocument;
			if(parsed_doc == null)
				return false;
			
			var parsed_file = parsed_doc.ParsedFile as BVE5UnresolvedFile;
			
			if(parsed_file == null)
				return false;

			try{
				result = ResolveAtLocation.Resolve(new Lazy<ICompilation>(() => doc.Compilation), parsed_file, unit, loc, out file);
				if(result == null || file is Statement)
					return false;
			}catch(Exception e){
				Console.WriteLine("Got resolver exception:" + e);
				return false;
			}
			return true;
		}
	}
}

