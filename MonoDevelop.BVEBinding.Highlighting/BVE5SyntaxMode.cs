// 
// SyntaxMode.cs
//  
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Threading;
using System.Diagnostics;

using Mono.TextEditor.Highlighting;
using Mono.TextEditor;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.SourceEditor.QuickTasks;

using BVE5Language.Ast;
using BVE5Language.Resolver;
using BVE5Language.TypeSystem;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;

namespace MonoDevelop.BVEBinding.Highlighting
{
	static class StringHelper
	{
		public static bool IsAt(this string str, int idx, string pattern)
		{
			if(idx + pattern.Length > str.Length)
				return false;

			for(int i = 0; i < pattern.Length; i++)
				if (pattern [i] != str [idx + i])
					return false;

			return true;
		}
	}
	
	public class BVE5SyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode, IQuickTaskProvider
	{
		Document gui_doc;
		SyntaxTree unit;
		BVE5UnresolvedFile parsed_file;
		ICompilation compilation;
		BVE5AstResolver resolver;
		CancellationTokenSource src = null;

		internal class StyledTreeSegment : TreeSegment
		{
			public string Style {
				get;
				private set;
			}
			
			public StyledTreeSegment(int offset, int length, string style) : base(offset, length)
			{
				Style = style;
			}
		}
		
		class HighlightingSegmentTree : SegmentTree<StyledTreeSegment>
		{
			public bool GetStyle(Chunk chunk, ref int endOffset, out string style)
			{
				var segment = GetSegmentsAt(chunk.Offset).FirstOrDefault(s => s.Offset == chunk.Offset);
				if(segment == null){
					style = null;
					return false;
				}
				endOffset = segment.EndOffset;
				style = segment.Style;
				return true;
			}
			
			public void AddStyle(int startOffset, int endOffset, string style)
			{
				if(IsDirty)
					return;

				Add(new StyledTreeSegment(startOffset, endOffset - startOffset, style));
			}
		}
		
		HighlightingSegmentTree highlighted_segment_cache = new HighlightingSegmentTree();
		
		public bool DisableConditionalHighlighting {
			get;
			set;
		}
		
		protected override void OnDocumentSet(EventArgs e)
		{
			if(gui_doc != null){
				gui_doc.DocumentParsed -= HandleDocumentParsed;
				highlighted_segment_cache.RemoveListener(gui_doc.Editor.Document);
			}
			gui_doc = null;
			
			base.OnDocumentSet(e);
		}
		
		void HandleDocumentParsed(object sender, EventArgs e)
		{
			if(src != null)
				src.Cancel();

			resolver = null;
			if(gui_doc != null/* && MonoDevelop.Core.PropertyService.Get("EnableSemanticHighlighting", true)*/){
				var parsed_doc = gui_doc.ParsedDocument;
				if(parsed_doc != null){
					unit = parsed_doc.GetAst<SyntaxTree>();
					parsed_file = parsed_doc.ParsedFile as BVE5UnresolvedFile;

					if(gui_doc.Project != null && gui_doc.IsCompileableInProject){
						src = new CancellationTokenSource();
						var cancellatation_token = src.Token;
						System.Threading.Tasks.Task.Factory.StartNew(delegate {
							Thread.Sleep (100);
							compilation = gui_doc.Compilation;
							var new_resolver = new BVE5AstResolver(compilation, unit, parsed_file);
							var visitor = new QuickTaskVisitor(new_resolver, cancellatation_token);
							try{
								unit.AcceptWalker(visitor);
							}catch (Exception){
								return;
							}
							if (!cancellatation_token.IsCancellationRequested) {
								Gtk.Application.Invoke(delegate {
									if (cancellatation_token.IsCancellationRequested)
										return;

									var editor_date = gui_doc.Editor;
									if (editor_date == null)
										return;

									resolver = new_resolver;
									quick_tasks = visitor.QuickTasks;
									OnTasksUpdated(EventArgs.Empty);
									var textEditor = editor_date.Parent;
									if(textEditor != null){
										var margin = textEditor.TextViewMargin;
										if(!parsed_doc.HasErrors){
											highlighted_segment_cache.Clear();
											margin.PurgeLayoutCache();
											textEditor.QueueDraw();
										}
									}
								});
							}
						}, cancellatation_token);
					}
				}
			}
		}

		class QuickTaskVisitor : AstWalker
		{
			internal List<QuickTask> QuickTasks = new List<QuickTask>();
			private readonly BVE5AstResolver resolver;
			private readonly CancellationToken cancellation_token;

			public QuickTaskVisitor(BVE5AstResolver resolver, CancellationToken cancellationToken)
			{
				this.resolver = resolver;
			}
			
			protected bool Walk(AstNode node)
			{
				if(cancellation_token.IsCancellationRequested)
					return false;

				return true;
			}

			public override bool Walk(Identifier node)
			{
				//base.VisitIdentifierExpression(identifierExpression);
				
				var result = resolver.Resolve(node, cancellation_token);
				if(result.IsError)
					QuickTasks.Add(new QuickTask(string.Format("error CS0103: The name `{0}' does not exist in the current context", node.Name),
					                             node.StartLocation, ICSharpCode.NRefactory.CSharp.Severity.Error));

				return false;
			}

			public override bool Walk(MemberReferenceExpression node)
			{
				//base.VisitMemberReferenceExpression(memberReferenceExpression);
				var result = resolver.Resolve(node, cancellation_token) as UnknownMemberResolveResult;
				if(result != null && result.TargetType.Kind != TypeKind.Unknown)
					QuickTasks.Add(new QuickTask(string.Format("error CS0117: `{0}' does not contain a definition for `{1}'", result.TargetType.FullName, node.Reference.Name),
					                             node.Reference.StartLocation, ICSharpCode.NRefactory.CSharp.Severity.Error));

				return true;
			}
		}
		
		static BVE5SyntaxMode()
		{
			IdeApp.Workspace.ActiveConfigurationChanged += delegate {
				foreach(var doc in IdeApp.Workbench.Documents){
					TextEditorData data = doc.Editor;
					if(data == null)
						continue;
					// Force syntax mode reparse (required for #if directives)
					doc.Editor.Document.SyntaxMode = doc.Editor.Document.SyntaxMode;
					doc.ReparseDocument();
				}
			};
		}
		
		public BVE5SyntaxMode()
		{
			var provider = new ResourceXmlProvider(typeof(BVE5SyntaxMode).Assembly,
			                                       typeof(BVE5SyntaxMode).Assembly.GetManifestResourceNames().First(s => s.Contains("BVE5SyntaxMode")));
			using(XmlReader reader = provider.Open()){
				SyntaxMode base_mode = SyntaxMode.Read(reader);
				rules = new List<Rule>(base_mode.Rules);
				keywords = new List<Keywords>(base_mode.Keywords);
				spans = base_mode.Spans;
				matches = base_mode.Matches;
				prevMarker = base_mode.PrevMarker;
				SemanticRules = new List<SemanticRule>(base_mode.SemanticRules);
				keywordTable = base_mode.keywordTable;
				keywordTableIgnoreCase = base_mode.keywordTableIgnoreCase;
				properties = base_mode.Properties;
			}
			
			AddSemanticRule("Comment", new HighlightUrlSemanticRule("comment"));
		}
		
		void EnsureGuiDocument()
		{
			if(gui_doc != null)
				return;

			try{
				if(File.Exists(Document.FileName))
					gui_doc = IdeApp.Workbench.GetDocument(Document.FileName);
			}catch(Exception){
				gui_doc = null;
			}

			if(gui_doc != null){
				gui_doc.Closed += delegate {
					if(src != null)
						src.Cancel();
				};

				gui_doc.DocumentParsed += HandleDocumentParsed;
				highlighted_segment_cache = new HighlightingSegmentTree();
				highlighted_segment_cache.InstallListener(gui_doc.Editor.Document);
				if(gui_doc.ParsedDocument != null)
					HandleDocumentParsed(this, EventArgs.Empty);
			}
		}
		
		public override SpanParser CreateSpanParser(DocumentLine line, CloneableStack<Span> spanStack)
		{
			EnsureGuiDocument();
			return new BVE5SpanParser(this, spanStack ?? line.StartSpan.Clone());
		}
		
		public override ChunkParser CreateChunkParser(SpanParser spanParser, ColorScheme style, DocumentLine line)
		{
			EnsureGuiDocument();
			return new BVE5ChunkParser(this, spanParser, style, line);
		}
		
		abstract class AbstractBlockSpan : Span
		{
			public bool IsValid {
				get;
				private set;
			}
			
			bool disabled;
			
			public bool Disabled {
				get { return disabled; }
				set { disabled = value; SetColor(); }
			}
			
			
			public AbstractBlockSpan(bool isValid)
			{
				IsValid = isValid;
				SetColor ();
				StopAtEol = false;
			}
			
			protected void SetColor()
			{
				TagColor = "text.preprocessor";
				if (disabled || !IsValid) {
					Color = "comment.block";
					Rule = "PreProcessorComment";
				} else {
					Color = "text";
					Rule = "<root>";
				}
			}
		}

		class DefineSpan : Span
		{
			string define;

			public string Define { 
				get { 
					return define;
				}
			}

			public DefineSpan (string define)
			{
				this.define = define;
				StopAtEol = false;
				Color = "text";
				Rule = "<root>";
			}
		}

		protected class BVE5ChunkParser : ChunkParser
		{
			HashSet<string> tags = new HashSet<string>();
			/*
			sealed class SemanticResolveVisitorNavigator : IResolveVisitorNavigator
			{
				readonly Dictionary<AstNode, ResolveVisitorNavigationMode> dict = new Dictionary<AstNode, ResolveVisitorNavigationMode> ();
				
				public void AddNode (AstNode node)
				{
					dict [node] = ResolveVisitorNavigationMode.Resolve;
					for (var ancestor = node.Parent; ancestor != null && !dict.ContainsKey(ancestor); ancestor = ancestor.Parent) {
						dict.Add (ancestor, ResolveVisitorNavigationMode.Scan);
					}
				}
				
				public void ProcessConversion (Expression expression, ResolveResult result, Conversion conversion, IType targetType)
				{
					
				}
				
				public void Resolved (AstNode node, ResolveResult result)
				{

				}
				
				public ResolveVisitorNavigationMode Scan (AstNode node)
				{
					if (node is Expression || node is AstType)
						return ResolveVisitorNavigationMode.Resolve;
					return ResolveVisitorNavigationMode.Scan;
				}
				
				public void Reset ()
				{
					dict.Clear ();
				}
			}*/
			BVE5SyntaxMode bve_syntax_mode;
			
			public BVE5ChunkParser(BVE5SyntaxMode bve5SyntaxMode, SpanParser spanParser, ColorScheme style, DocumentLine line) : base (bve5SyntaxMode, spanParser, style, line)
			{
				this.bve_syntax_mode = bve5SyntaxMode;
				foreach(var tag in CommentTag.SpecialCommentTags)
					tags.Add(tag.Tag);
			}

			string GetSemanticStyle(ParsedDocument parsedDocument, Chunk chunk, ref int endOffset)
			{
				string style;
				bool found = bve_syntax_mode.highlighted_segment_cache.GetStyle(chunk, ref endOffset, out style);
				if(!found && !bve_syntax_mode.highlighted_segment_cache.IsDirty){
					style = GetSemanticStyleFromAst(parsedDocument, chunk, ref endOffset);
					bve_syntax_mode.highlighted_segment_cache.AddStyle(chunk.Offset, style == null ? chunk.EndOffset : endOffset, style);
				}
				return style;
			}

			static int TokenLength(AstNode node)
			{
				Debug.Assert(node.StartLocation.Line == node.EndLocation.Line);
				return node.EndLocation.Column - node.StartLocation.Column;
			}

			string GetSemanticStyleFromAst(ParsedDocument parsedDocument, Chunk chunk, ref int endOffset)
			{
				var unit = bve_syntax_mode.unit;
				if(unit == null || bve_syntax_mode.resolver == null)
					return null;
				
				var loc = doc.OffsetToLocation(chunk.Offset);
				var node = unit.GetNodeAt(loc, n => n is Identifier);

				while(node != null && !(node is Statement)){
					if(node is Identifier){
						if(node.Parent is IndexerExpression){
							endOffset = chunk.Offset + TokenLength((Identifier)node);
							return "keyword.semantic.nametype";
						}

						if(node.Parent is MemberReferenceExpression){
							endOffset = chunk.Offset + TokenLength((Identifier)node);
							return "keyword.semantic";
						}
					}
					
					var memberReferenceExpression = node as MemberReferenceExpression;
					if(memberReferenceExpression != null){
						if(!memberReferenceExpression.Reference.Contains(loc)) 
							return null;
						
						var result = bve_syntax_mode.resolver.Resolve(memberReferenceExpression);
						if(result.IsError && bve_syntax_mode.gui_doc.Project != null){
							endOffset = chunk.Offset + TokenLength(memberReferenceExpression.Reference);
							return "keyword.semantic.error";
						}
						
						if(result is MemberResolveResult){
							var member = ((MemberResolveResult)result).Member;
							if(member is IField){
								endOffset = chunk.Offset + TokenLength(memberReferenceExpression.Reference);
								return "keyword.semantic.field";
							}
						}
					}

					node = node.Parent;
				}

				return null;
			}
			
			protected override void AddRealChunk(Chunk chunk)
			{
				var document = bve_syntax_mode.gui_doc;
				var parsedDocument = document != null ? document.ParsedDocument : null;
				if(parsedDocument != null){
					int end_loc = -1;
					string semantic_style = null;

					if(spanParser.CurSpan == null || spanParser.CurSpan is DefineSpan){
						try{
							semantic_style = GetSemanticStyle (parsedDocument, chunk, ref end_loc);
						}catch(Exception e){
							Console.WriteLine("Error in semantic highlighting: " + e);
						}
					}

					if(semantic_style != null){
						if(end_loc < chunk.EndOffset){
							base.AddRealChunk(new Chunk(chunk.Offset, end_loc - chunk.Offset, semantic_style));
							base.AddRealChunk(new Chunk(end_loc, chunk.EndOffset - end_loc, chunk.Style));
							return;
						}
						chunk.Style = semantic_style;
					}
				}
				
				base.AddRealChunk(chunk);
			}
			
			protected override string GetStyle(Chunk chunk)
			{
				if(spanParser.CurRule.Name == "Comment"){
					if(tags.Contains(doc.GetTextAt(chunk))) 
						return "comment.keyword.todo";
				}

				return base.GetStyle(chunk);
			}
		}
		
		protected class BVE5SpanParser : SpanParser
		{
			BVE5SyntaxMode BVE5SyntaxMode{
				get {
					return (BVE5SyntaxMode)mode;
				}
			}

			IEnumerable<string> Defines {
				get {
					if (SpanStack == null)
						yield break;
					foreach (var span in SpanStack) {
						if (span is DefineSpan) {
							var define = ((DefineSpan)span).Define;
							if (define != null)
								yield return define;
						}
					}
				}
			}

			protected override void ScanSpan(ref int i)
			{
				if(BVE5SyntaxMode.DisableConditionalHighlighting){
					base.ScanSpan (ref i);
					return;
				}
				int textOffset = i - StartOffset;

				if (textOffset < CurText.Length && CurRule.Name != "Comment" && CurRule.Name != "String" && CurText [textOffset] == '#' && IsFirstNonWsChar (textOffset)) {

					if (CurText.IsAt (textOffset, "#define")) {
						int length = CurText.Length - textOffset;
						string parameter = CurText.Substring (textOffset + "#define".Length, length - "#define".Length).Trim ();

						var defineSpan = new DefineSpan (parameter);
						FoundSpanBegin (defineSpan, i, 0);
					}
	
					var preprocessorSpan = CreatePreprocessorSpan ();
					FoundSpanBegin (preprocessorSpan, i, 1);
					return;
				}

				base.ScanSpan (ref i);
			}
			
			public static Span CreatePreprocessorSpan()
			{
				var result = new Span ();
				result.TagColor = "text.preprocessor";
				result.Color = "text.preprocessor";
				result.Rule = "String";
				result.StopAtEol = true;
				return result;
			}
			
			void PopCurrentIfBlock()
			{
				/*while (spanStack.Count > 0 && (spanStack.Peek () is IfBlockSpan || spanStack.Peek () is ElseIfBlockSpan || spanStack.Peek () is ElseBlockSpan)) {
					var poppedSpan = PopSpan ();
					if (poppedSpan is IfBlockSpan)
						break;
				}*/
			}
			
			protected override bool ScanSpanEnd(Mono.TextEditor.Highlighting.Span cur, ref int i)
			{
				/*if (cur is IfBlockSpan || cur is ElseIfBlockSpan || cur is ElseBlockSpan) {
					int textOffset = i - StartOffset;
					bool end = CurText.IsAt (textOffset, "#endif");
					if (end) {
						FoundSpanEnd (cur, i, 6); // put empty end tag in
						
						// if we're in a complex span stack pop it up to the if block
						if (spanStack.Count > 0) {
							var prev = spanStack.Peek ();
							
							if ((cur is ElseIfBlockSpan || cur is ElseBlockSpan) && (prev is ElseIfBlockSpan || prev is IfBlockSpan))
								PopCurrentIfBlock ();
						}
					}
					return end;
				}*/
				return base.ScanSpanEnd (cur, ref i);
			}
			
	//		Span preprocessorSpan;
	//		Rule preprocessorRule;
			
			public BVE5SpanParser (BVE5SyntaxMode mode, CloneableStack<Span> spanStack) : base (mode, spanStack)
			{
//				foreach (Span span in mode.Spans) {
//					if (span.Rule == "text.preprocessor") {
//						preprocessorSpan = span;
//						preprocessorRule = GetRule (span);
//					}
//				}
			}
		}

		#region IQuickTaskProvider implementation
		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated(System.EventArgs e)
		{
			var handler = TasksUpdated;
			if(handler != null)
				handler(this, e);
		}

		List<QuickTask> quick_tasks;
		public IEnumerable<QuickTask> QuickTasks {
			get {
				return quick_tasks;
			}
		}
		#endregion
	}
}
 