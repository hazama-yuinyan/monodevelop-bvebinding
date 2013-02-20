using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;


namespace MonoDevelop.BVEBinding
{
	public class BVELanguageBinding : ILanguageBinding
	{
		public string Language{
			get {return "BVE5";}
		}

		public string SingleLineCommentTag{get{return "//";}}
		public string BlockCommentStartTag{get{return null;}}
		public string BlockCommentEndTag{get{return null;}}

		public bool IsSourceCodeFile(FilePath fileName)
		{
			if(fileName.ToString().EndsWith(".txt", StringComparison.OrdinalIgnoreCase)){
				using(var fs = new StreamReader(fileName)){
					return fs.ReadLine().Contains("BveTs Map 1.00");
				}
			}

			return false;
		}

		public FilePath GetFileName(FilePath baseName)
		{
			return baseName + ".txt";
		}
	}
}

