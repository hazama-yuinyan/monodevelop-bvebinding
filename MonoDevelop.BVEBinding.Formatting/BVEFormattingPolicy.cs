//
// ExpressoFormattingPolicy.cs
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
using System.Reflection;
using System.Xml;
using System.Linq;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Policies;
using ICSharpCode.NRefactory.CSharp;


namespace MonoDevelop.BVEBinding.Formatting
{
	[PolicyType("BVE5 formatting")]
	public class BVE5FormattingPolicy : IEquatable<BVE5FormattingPolicy>
	{
		public string Name{
			get;
			set;
		}

		public bool IsBuiltin{
			get;
			set;
		}

		static BVE5FormattingPolicy()
		{
			PolicyService.InvariantPolicies.Set<BVE5FormattingPolicy>(new BVE5FormattingPolicy(), "text/x-bve5");
		}

		public BVE5FormattingPolicy()
		{
		}

		#region Indentation
		[ItemProperty]
		public bool IndentNonPositionStatements{
			get; set;
		}
		
		[ItemProperty]
		public bool AllowMultipleStatementsInLine{
			get; set;
		}
		#endregion

		public static BVE5FormattingPolicy Load(System.IO.Stream input)
		{
			var result = new BVE5FormattingPolicy();
			result.Name = "noname";
			using(XmlTextReader reader = new XmlTextReader(input)){
				while(reader.Read()){
					if(reader.NodeType == XmlNodeType.Element){
						if(reader.LocalName == "Property"){
							var info = typeof(BVE5FormattingPolicy).GetProperty(reader.GetAttribute("name"));
							string valString = reader.GetAttribute("value");
							object value;
							if (info.PropertyType == typeof (bool)){
								value = Boolean.Parse(valString);
							}else if(info.PropertyType == typeof (int)){
								value = Int32.Parse(valString);
							}else{
								value = Enum.Parse(info.PropertyType, valString);
							}
							info.SetValue(result, value, null);
						}else if(reader.LocalName == "FormattingProfile"){
							result.Name = reader.GetAttribute("name");
						}
					}else if(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "FormattingProfile"){
						//Console.WriteLine ("result:" + result.Name);
						return result;
					}
				}
			}
			return result;
		}
		
		public void Save(string fileName)
		{
			using (var writer = new XmlTextWriter(fileName, Encoding.Default)){
				writer.Formatting = System.Xml.Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				writer.WriteStartElement("FormattingProfile");
				writer.WriteAttributeString("name", Name);
				foreach(PropertyInfo info in typeof(BVE5FormattingPolicy).GetProperties()){
					if (info.GetCustomAttributes(false).Any(o => o.GetType() == typeof(ItemPropertyAttribute))) {
						writer.WriteStartElement("Property");
						writer.WriteAttributeString("name", info.Name);
						writer.WriteAttributeString("value", info.GetValue (this, null).ToString());
						writer.WriteEndElement();
					}
				}
				writer.WriteEndElement();
			}
		}
		
		public bool Equals(BVE5FormattingPolicy other)
		{
			foreach(PropertyInfo info in typeof(BVE5FormattingPolicy).GetProperties()){
				if(info.GetCustomAttributes(false).Any(o => o.GetType() == typeof(ItemPropertyAttribute))){
					object val = info.GetValue(this, null);
					object otherVal = info.GetValue(other, null);
					if(val == null){
						if(otherVal == null)
							continue;

						return false;
					}
					if(!val.Equals (otherVal)){
						//Console.WriteLine ("!equal");
						return false;
					}
				}
			}
			//Console.WriteLine ("== equal");
			return true;
		}
	}
}

