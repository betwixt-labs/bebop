using System;
namespace Core.Generators
{
	/// <summary>
	/// An enum that defines which parts of a service are generated 
	/// </summary>
	public enum XrpcServices
	{
		/// <summary>
		/// Indicates no service code should be generated 
		/// </summary>
		None = 0,
		/// <summary>
		/// Indicates only client service code should be generated
		/// </summary>
		Client = 1,
		/// <summary>
		/// Indicates only server service code should be generated
		/// </summary>
		Server = 2,
		/// <summary>
		/// Indicates both client and server service code should be generated
		/// </summary>
		Both = 3
		
	}
}

