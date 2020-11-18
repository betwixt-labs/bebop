using System;
using JetBrains.Annotations;

namespace Bebop.Attributes
{
    /// <summary>
    /// Binds the specified <see cref="RecordType"/> to the attributed method for use with <see cref="Runtime.BebopWindow"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [MeansImplicitUse]
    public class BindRecordAttribute : Attribute
    {

        public BindRecordAttribute(Type recordType)
        {
            RecordType = recordType;
        }

        public Type RecordType { get; init; }
    }
}