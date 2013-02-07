using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

using BVE5Language.TypeSystem;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// Contains the main resolver logic.
	/// </summary>
	/// <remarks>
	/// This class is thread-safe.
	/// </remarks>
	public class BVE5Resolver
	{
		static readonly ResolveResult ErrorResult = ErrorResolveResult.UnknownError;
		static readonly string[] TypeNames;
		static readonly Dictionary<string, string[]> MethodNames;
		private readonly ICompilation compilation;
		private BVE5TypeResolveContext context;
		
		public class BuiltinsDefinition
		{
			public string[] Types{get; set;}
			public Dictionary<string, string[]> Methods{get; set;}
		}

		public ICompilation Compilation{
			get{return compilation;}
		}

		public BVE5TypeResolveContext CurrentTypeResolveContext{
			get{return context;}
		}
		
		#region Constructor
		static BVE5Resolver()
		{
			var builtin_names = JsonConvert.DeserializeObject<BuiltinsDefinition>(File.ReadAllText("./resources/BVE5BuiltinNames.json"));
			TypeNames = builtin_names.Types;
			MethodNames = builtin_names.Methods;
		}
		
		public BVE5Resolver(ICompilation compilation)
		{
			if(compilation == null)
				throw new ArgumentNullException("compilation");
			
			
		}
		#endregion
	}
}

