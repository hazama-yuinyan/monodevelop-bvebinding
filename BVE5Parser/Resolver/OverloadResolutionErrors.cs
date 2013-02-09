using System;

namespace BVE5Language.Resolver
{
	[Flags]
	public enum OverloadResolutionErrors
	{
		None = 0,
		/// <summary>
		/// Too many positional arguments (some could not be mapped to any parameter).
		/// </summary>
		TooManyPositionalArguments = 0x0001,
		/// <summary>
		/// After substituting type parameters with the inferred types; a constructed type within the formal parameters
		/// does not satisfy its constraint.
		/// </summary>
		ConstructedTypeDoesNotSatisfyConstraint = 0x0002,
		/// <summary>
		/// No argument was mapped to a non-optional parameter
		/// </summary>
		MissingArgumentForRequiredParameter = 0x0004,
		/// <summary>
		/// Several arguments were mapped to a single (non-params-array) parameter
		/// </summary>
		MultipleArgumentsForSingleParameter = 0x0008,
		/// <summary>
		/// Argument type cannot be converted to parameter type
		/// </summary>
		ArgumentTypeMismatch = 0x0010,
		/// <summary>
		/// There is no unique best overload.
		/// This error does not apply to any single candidate, but only to the overall result of overload resolution.
		/// </summary>
		/// <remarks>
		/// This error does not prevent a candidate from being applicable.
		/// </remarks>
		AmbiguousMatch = 0x0020,
	}
}

