//
// BVE5UnresolvedFile.cs
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
using System.Collections.Generic;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.TypeSystem
{
	public class BVE5UnresolvedFile : IUnresolvedFile
	{
		private readonly string file_name;
		private List<Error> errors;

		public BVE5UnresolvedFile(string fileName, List<Error> errorList)
		{
			file_name = fileName;
			errors = errorList;
		}

		#region IUnresolvedFile implementation
		public IUnresolvedTypeDefinition GetTopLevelTypeDefinition(TextLocation location)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedTypeDefinition GetInnermostTypeDefinition(TextLocation location)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedMember GetMember(TextLocation location)
		{
			throw new NotImplementedException ();
		}

		public ITypeResolveContext GetTypeResolveContext(ICompilation compilation, TextLocation loc)
		{
			throw new NotImplementedException ();
		}

		public string FileName {
			get {
				return file_name;
			}
		}

		public DateTime? LastWriteTime{get;set;}

		public IList<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
			get {
				throw new NotImplementedException("Never implemented!");
			}
		}

		public IList<IUnresolvedAttribute> AssemblyAttributes {
			get {
				throw new NotImplementedException("Never implemented!");
			}
		}

		public IList<IUnresolvedAttribute> ModuleAttributes {
			get {
				throw new NotImplementedException("Never implemented!");
			}
		}

		public IList<Error> Errors {
			get {
				return errors;
			}
		}
		#endregion
	}
}

