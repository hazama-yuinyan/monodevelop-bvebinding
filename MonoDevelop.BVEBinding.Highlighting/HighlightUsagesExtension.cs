// 
// HighlightUsagesExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using Mono.TextEditor;

using Gdk;

using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.QuickTasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;

using BVE5Language.Ast;
using MonoDevelop.BVEBinding.Resolver;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.CSharp.Highlighting
{
	public class HighlightUsagesExtension : TextEditorExtension, IUsageProvider
	{
		public readonly List<TextSegment> UsagesSegments = new List<TextSegment>();
			
		TextEditorData text_editor_data;

		public override void Initialize()
		{
			base.Initialize();
			
			text_editor_data = base.Document.Editor;
			text_editor_data.Caret.PositionChanged += HandleTextEditorDataCaretPositionChanged;
			text_editor_data.Document.TextReplaced += HandleTextEditorDataDocumentTextReplaced;
			text_editor_data.SelectionChanged += HandleTextEditorDataSelectionChanged;
		}

		void HandleTextEditorDataSelectionChanged(object sender, EventArgs e)
		{
			RemoveMarkers(false);
		}

		void HandleTextEditorDataDocumentTextReplaced(object sender, DocumentChangeEventArgs e)
		{
			RemoveMarkers(false);
		}
		
		public override void Dispose()
		{
			text_editor_data.SelectionChanged -= HandleTextEditorDataSelectionChanged;
			text_editor_data.Caret.PositionChanged -= HandleTextEditorDataCaretPositionChanged;
			text_editor_data.Document.TextReplaced -= HandleTextEditorDataDocumentTextReplaced;
			base.Dispose();
			RemoveTimer();
		}
		
		uint popup_timer = 0;
		
		public bool IsTimerOnQueue {
			get {
				return popup_timer != 0;
			}
		}
		
		public void ForceUpdate()
		{
			RemoveTimer();
			DelayedTooltipShow();
		}
		
		void RemoveTimer()
		{
			if(popup_timer != 0){
				GLib.Source.Remove(popup_timer);
				popup_timer = 0;
			}
		}

		void HandleTextEditorDataCaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if(!SourceEditor.DefaultSourceEditorOptions.Instance.EnableHighlightUsages)
				return;

			if(!text_editor_data.IsSomethingSelected && markers.Values.Any(m => m.Contains(text_editor_data.Caret.Offset)))
				return;

			RemoveMarkers(text_editor_data.IsSomethingSelected);
			RemoveTimer();
			if(!text_editor_data.IsSomethingSelected)
				popup_timer = GLib.Timeout.Add(1000, DelayedTooltipShow);
		}

		void ClearQuickTasks()
		{
			UsagesSegments.Clear();
			if(usages.Count > 0){
				usages.Clear();
				OnUsagesUpdated(EventArgs.Empty);
			}
		}

		bool DelayedTooltipShow()
		{
			try{
				ResolveResult result;
				AstNode node;

				if(!Document.TryResolveAt(Document.Editor.Caret.Location, out result, out node)){
					ClearQuickTasks();
					return false;
				}
				/*if(node is PrimitiveType){
					ClearQuickTasks();
					return false;
				}*/

				ShowReferences(GetReferences(result));
			}catch(Exception e){
				LoggingService.LogError("Unhandled Exception in HighlightingUsagesExtension", e);
			}finally{
				popup_timer = 0;
			}
			return false;
		}
		
		void ShowReferences(IEnumerable<MemberReference> references)
		{
			RemoveMarkers(false);
			var line_nums = new HashSet<int>();
			usages.Clear();
			UsagesSegments.Clear();

			var editor = text_editor_data.Parent;
			if(editor != null && editor.TextViewMargin != null){
				if(references != null){
					bool alpha_blend = false;
					foreach(var r in references){
						var marker = GetMarker(r.Region.BeginLine);
						
						usages.Add(r.Region.Begin);
						
						int offset = r.Offset;
						int end_offset = offset + r.Length;
						if(!alpha_blend && editor.TextViewMargin.SearchResults.Any(sr => sr.Contains(offset) || sr.Contains(end_offset) ||
							offset < sr.Offset && sr.EndOffset < end_offset)){
							editor.TextViewMargin.AlphaBlendSearchResults = alpha_blend = true;
						}
						UsagesSegments.Add(new TextSegment(offset, end_offset - offset));
						marker.Usages.Add(new TextSegment(offset, end_offset - offset));
						line_nums.Add(r.Region.BeginLine);
					}
				}

				foreach(int line in line_nums)
					text_editor_data.Document.CommitLineUpdate(line);

				UsagesSegments.Sort((x, y) => x.Offset.CompareTo(y.Offset));
			}

			OnUsagesUpdated(EventArgs.Empty);
		}


		static readonly List<MemberReference> EmptyList = new List<MemberReference>();
		IEnumerable<MemberReference> GetReferences(ResolveResult resolveResult)
		{
			/*var finder = new MonoDevelop.BVEBinding.Refactoring.BVE5ReferenceFinder();
			if (resolveResult is MemberResolveResult) {
				finder.SetSearchedMembers (new [] { ((MemberResolveResult)resolveResult).Member });
			} else if (resolveResult is TypeResolveResult) {
				finder.SetSearchedMembers (new [] { resolveResult.Type });
			} else if (resolveResult is MethodGroupResolveResult) { 
				finder.SetSearchedMembers (((MethodGroupResolveResult)resolveResult).Methods);
			} else if (resolveResult is NamespaceResolveResult) { 
				finder.SetSearchedMembers (new [] { ((NamespaceResolveResult)resolveResult).NamespaceName });
			} else if (resolveResult is LocalResolveResult) { 
				finder.SetSearchedMembers (new [] { ((LocalResolveResult)resolveResult).Variable });
			} else {
				return EmptyList;
			}
			
			try{
				return new List<MemberReference>(finder.FindInDocument(Document));
			}catch(Exception e){
				LoggingService.LogError("Error in highlight usages extension.", e);
			}*/
			return EmptyList;
		}

		Dictionary<int, UsageMarker> markers = new Dictionary<int, UsageMarker>();
		
		public Dictionary<int, UsageMarker> Markers {
			get { return this.markers; }
		}
		
		void RemoveMarkers(bool updateLine)
		{
			if (markers.Count == 0)
				return;

			text_editor_data.Parent.TextViewMargin.AlphaBlendSearchResults = false;
			foreach(var pair in markers)
				text_editor_data.Document.RemoveMarker(pair.Value, true);

			markers.Clear();
		}
		
		UsageMarker GetMarker(int line)
		{
			UsageMarker result;
			if(!markers.TryGetValue(line, out result)){
				result = new UsageMarker();
				text_editor_data.Document.AddMarker(line, result);
				markers.Add(line, result);
			}

			return result;
		}

		
		public class UsageMarker : TextMarker, IBackgroundMarker
		{
			List<TextSegment> usages = new List<TextSegment> ();

			public List<TextSegment> Usages {
				get { return this.usages; }
			}
			
			public bool Contains(int offset)
			{
				return usages.Any(u => u.Offset <= offset && offset <= u.EndOffset);
			}
			
			public bool DrawBackground(TextEditor editor, Cairo.Context cr, TextViewMargin.LayoutWrapper layout, int selectionStart,
			                           int selectionEnd, int startOffset, int endOffset, double y, double startXPos, double endXPos, ref bool drawBg)
			{
				drawBg = false;
				if (selectionStart >= 0 || editor.CurrentMode is TextLinkEditMode)
					return true;

				foreach(var usage in Usages){
					int marker_start = usage.Offset;
					int marker_end = usage.EndOffset;
					
					if(marker_end < startOffset || marker_start > endOffset) 
						return true; 
					
					double @from;
					double to;
					
					if(marker_start < startOffset && endOffset < marker_end){
						@from = startXPos;
						to = endXPos;
					}else{
						int start = startOffset < marker_start ? marker_start : startOffset;
						int end = endOffset < marker_end ? endOffset : marker_end;
						
						uint curIndex = 0, byteIndex = 0;
						TextViewMargin.TranslateToUTF8Index(layout.LineChars, (uint)(start - startOffset), ref curIndex, ref byteIndex);
						
						int x_pos = layout.Layout.IndexToPos((int)byteIndex).X;
						
						@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
						
						TextViewMargin.TranslateToUTF8Index(layout.LineChars, (uint)(end - startOffset), ref curIndex, ref byteIndex);
						x_pos = layout.Layout.IndexToPos((int)byteIndex).X;
			
						to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
					}
		
					@from = System.Math.Max(@from, editor.TextViewMargin.XOffset);
					to = System.Math.Max(to, editor.TextViewMargin.XOffset);
					if(@from < to){
						cr.Color = (HslColor)editor.ColorStyle.UsagesHighlightRectangle.BackgroundColor;
						cr.Rectangle(@from + 1, y + 1, to - @from - 1, editor.LineHeight - 2);
						cr.Fill();
						
						cr.Color = (HslColor)editor.ColorStyle.UsagesHighlightRectangle.Color;
						cr.Rectangle(@from + 0.5, y + 0.5, to - @from, editor.LineHeight - 1);
						cr.Stroke();
					}
				}
				return true;
			}
		}

		#region IUsageProvider implementation
		public event EventHandler UsagesUpdated;

		protected virtual void OnUsagesUpdated(System.EventArgs e)
		{
			EventHandler handler = this.UsagesUpdated;
			if(handler != null)
				handler (this, e);
		}

		List<DocumentLocation> usages = new List<DocumentLocation>();
		IEnumerable<DocumentLocation> IUsageProvider.Usages {
			get {
				return usages;
			}
		}
		#endregion
	}
}

