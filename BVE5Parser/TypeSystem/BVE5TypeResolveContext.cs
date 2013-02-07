using System;

using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.TypeSystem
{
	public sealed class BVE5TypeResolveContext : ITypeResolveContext
	{
		private readonly IAssembly assembly;
		private readonly ITypeDefinition current_type_def;
		private readonly IMember current_member;
		
		public BVE5TypeResolveContext(IAssembly assembly, ITypeDefinition typeDefinition = null, IMember member = null)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			this.assembly = assembly;
			this.current_type_def = typeDefinition;
			this.current_member = member;
		}
		
		public ICompilation Compilation {
			get { return assembly.Compilation; }
		}
		
		public IAssembly CurrentAssembly {
			get { return assembly; }
		}
		
		public ITypeDefinition CurrentTypeDefinition {
			get { return current_type_def; }
		}
		
		public IMember CurrentMember {
			get { return current_member; }
		}
		
		public BVE5TypeResolveContext WithCurrentTypeDefinition(ITypeDefinition typeDefinition)
		{
			return new BVE5TypeResolveContext(assembly, typeDefinition, current_member);
		}
		
		ITypeResolveContext ITypeResolveContext.WithCurrentTypeDefinition(ITypeDefinition typeDefinition)
		{
			return WithCurrentTypeDefinition(typeDefinition);
		}
		
		public BVE5TypeResolveContext WithCurrentMember(IMember member)
		{
			return new BVE5TypeResolveContext(assembly, current_type_def, member);
		}
		
		ITypeResolveContext ITypeResolveContext.WithCurrentMember(IMember member)
		{
			return WithCurrentMember(member);
		}
	}
}

