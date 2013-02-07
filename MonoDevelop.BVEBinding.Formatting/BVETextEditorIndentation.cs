using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using Mono.TextEditor;


namespace MonoDevelop.BVEBinding.Formatting
{
	public class BVE5TextEditorIndentation : TextEditorExtension
	{
		DocumentStateTracker<BVE5IndentEngine> state_tracker;
		int cursor_pos_before_keypress;
		TextEditorData editor_data;
		BVE5FormattingPolicy fomartting_policy;
		TextStylePolicy style_policy;

		char last_char_inserted;

		static BVE5TextEditorIndentation()
		{
		}
	}
}

