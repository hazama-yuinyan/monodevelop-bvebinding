//
// BVE5Ambience.cs
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

using ICSharpCode.NRefactory.TypeSystem;

using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.BVEBinding
{
	public class BVE5Ambience : Ambience
	{
		public BVE5Ambience() : base("BVE5")
		{
		}

		#region MonoDevelop.Ide.TypeSystem.Ambience members
		public override string GetIntrinsicTypeName(string reflectionName)
		{
			return "";
		}

		public override string SingleLineComment(string text)
		{
			return "//" + text;
		}

		public override string GetString(string nameSpace, OutputSettings settings)
		{
			return "namespace" + nameSpace;
		}

		protected override string GetTypeString(IType type, OutputSettings settings)
		{
			return "";
		}

		protected override string GetTypeReferenceString(IType reference, OutputSettings settings)
		{
			return "";
		}

		protected override string GetConstructorString(IMethod constructor, OutputSettings settings)
		{
			return "";
		}

		protected override string GetDestructorString(IMethod destructor, OutputSettings settings)
		{
			return "";
		}

		protected override string GetMethodString(IMethod method, OutputSettings settings)
		{
			if(method == null)
				return "";

			return settings.Markup(method.Name);
		}

		protected override string GetOperatorString(IMethod op, OutputSettings settings)
		{
			return "";
		}

		protected override string GetEventString(IEvent evt, OutputSettings settings)
		{
			return "";
		}

		protected override string GetFieldString(IField field, OutputSettings settings)
		{
			return "";
		}

		protected override string GetIndexerString(IProperty property, OutputSettings settings)
		{
			if(property == null)
				return "";

			return settings.Markup(property.Name);
		}

		protected override string GetPropertyString(IProperty property, OutputSettings settings)
		{
			return "";
		}

		protected override string GetParameterString(IParameterizedMember member, IParameter parameter, OutputSettings settings)
		{
			return "";
		}
		#endregion
	}
}

