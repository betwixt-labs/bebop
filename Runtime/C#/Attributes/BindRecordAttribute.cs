using System;

namespace Bebop.Attributes
{
    /// <summary>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class BindRecordAttribute : Attribute
    {
        public BindRecordAttribute(Type recordType)
        {
            RecordType = recordType;
        }

        public Type RecordType { get; init; }
    }
}