//
// ExpressoIndentEngine.cs
//
// Author:
//       HAZAMA <kotonechan@live.jp>
//
// Copyright (c) 2012 HAZAMA
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
using System.Linq;
using System.Text;

using MonoDevelop.Ide.Gui.Content;


namespace MonoDevelop.BVEBinding.Formatting
{
	public partial class BVE5IndentEngine : ICloneable, IDocumentStateEngine
	{
		IndentStack stack;
		
		// Ponder: should linebuf be dropped in favor of a
		// 'wordbuf' and a 'int curLineLen'? No real need to
		// keep a full line buffer.
		StringBuilder linebuf;
		
		string keyword, cur_indent;
		
		Inside began_inside;
		
		bool needs_reindent, pop_verbatim, can_be_label, is_escaped;
		
		int first_non_lwsp, last_non_lwsp, word_start;
		
		char last_char;
		
		// previous char in the line
		char pc;
		
		// last significant (real) char in the line
		// (e.g. non-whitespace, not in a comment, etc)
		char rc;
		
		// previous last significant (real) char in the line
		char prc;
		
		int cur_line_num, cursor;
		BVE5FormattingPolicy policy;
		TextStylePolicy textPolicy;

		public BVE5IndentEngine()
		{
		}

		public BVE5IndentEngine Clone()
		{
			return (BVE5IndentEngine)MemberwiseClone();
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		#region IDocumentStateEngine members
		public int Position{
			get{
				return cursor;
			}
		}

		public void Push(char input)
		{

		}

		public void Reset()
		{

		}
		#endregion
	}
}

