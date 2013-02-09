using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// A method list that belongs to a declaring type.
	/// </summary>
	public class MethodListWithDeclaringType : List<IParameterizedMember>
	{
		readonly IType declaringType;
		
		/// <summary>
		/// The declaring type.
		/// </summary>
		/// <remarks>
		/// Not all methods in this list necessarily have this as their declaring type.
		/// For example, this program:
		/// <code>
		///  class Base {
		///    public virtual void M() {}
		///  }
		///  class Derived : Base {
		///    public override void M() {}
		///    public void M(int i) {}
		///  }
		/// </code>
		/// results in two lists:
		///  <c>new MethodListWithDeclaringType(Base) { Derived.M() }</c>,
		///  <c>new MethodListWithDeclaringType(Derived) { Derived.M(int) }</c>
		/// </remarks>
		public IType DeclaringType {
			get { return declaringType; }
		}
		
		public MethodListWithDeclaringType(IType declaringType)
		{
			this.declaringType = declaringType;
		}
		
		public MethodListWithDeclaringType(IType declaringType, IEnumerable<IParameterizedMember> methods)
			: base(methods)
		{
			this.declaringType = declaringType;
		}
	}
	
	/// <summary>
	/// Represents a group of methods.
	/// A method reference used to create a delegate is resolved to a MethodGroupResolveResult.
	/// The MethodGroupResolveResult has no type.
	/// To retrieve the delegate type or the chosen overload, look at the method group conversion.
	/// </summary>
	public class MethodGroupResolveResult : ResolveResult
	{
		private readonly IList<MethodListWithDeclaringType> method_lists;
		private readonly IList<IType> type_arguments;
		private readonly ResolveResult target_result;
		private readonly string method_name;
		
		public MethodGroupResolveResult(ResolveResult targetResult, string methodName, IList<MethodListWithDeclaringType> methods,
		                                IList<IType> typeArguments) : base(SpecialType.UnknownType)
		{
			if(methods == null)
				throw new ArgumentNullException("methods");

			target_result = targetResult;
			method_name = methodName;
			method_lists = methods;
			type_arguments = typeArguments ?? EmptyList<IType>.Instance;
		}
		
		/// <summary>
		/// Gets the resolve result for the target object.
		/// </summary>
		public ResolveResult TargetResult {
			get { return target_result; }
		}
		
		/// <summary>
		/// Gets the type of the reference to the target object.
		/// </summary>
		public IType TargetType {
			get { return target_result != null ? target_result.Type : SpecialType.UnknownType; }
		}
		
		/// <summary>
		/// Gets the name of the methods in this group.
		/// </summary>
		public string MethodName {
			get { return method_name; }
		}
		
		/// <summary>
		/// Gets the methods that were found.
		/// This list does not include extension methods.
		/// </summary>
		public IEnumerable<IMethod> Methods {
			get { return method_lists.SelectMany(m => m.Cast<IMethod>()); }
		}
		
		/// <summary>
		/// Gets the methods that were found, grouped by their declaring type.
		/// This list does not include extension methods.
		/// Base types come first in the list.
		/// </summary>
		public IEnumerable<MethodListWithDeclaringType> MethodsGroupedByDeclaringType {
			get { return method_lists; }
		}
		
		/// <summary>
		/// Gets the type arguments that were explicitly provided.
		/// </summary>
		public IList<IType> TypeArguments {
			get { return type_arguments; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} with {1} method(s)]", GetType().Name, this.Methods.Count());
		}
		
		/*public OverloadResolution PerformOverloadResolution(ICompilation compilation, ResolveResult[] arguments, string[] argumentNames = null)
		{
			Log.WriteLine("Performing overload resolution for " + this);
			Log.WriteCollection("  Arguments: ", arguments);
			
			var typeArgumentArray = this.TypeArguments.ToArray();
			OverloadResolution or = new OverloadResolution(compilation, arguments, argumentNames, typeArgumentArray);
			
			or.AddMethodLists(method_lists);
			
			Log.WriteLine("Overload resolution finished, best candidate is {0}.", or.GetBestCandidateWithSubstitutedTypeArguments());
			return or;
		}*/
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if(target_result != null)
				return new[] { target_result };
			else
				return Enumerable.Empty<ResolveResult>();
		}
	}
}

