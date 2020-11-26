using System;
using Bebop.Runtime;

namespace Bebop.Attributes
{
    /// <summary>
    /// Binds the specified <see cref="RecordType"/> to the attributed method for use with <see cref="BebopMirror"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class BindRecordAttribute : Attribute
    {

        public BindRecordAttribute(Type recordType)
        {
            RecordType = recordType;
        }
        /// <summary>
        /// The <see cref="BebopRecord{T}"/> associated with the bound method.
        /// </summary>
        public Type RecordType { get; init; }
    }
}