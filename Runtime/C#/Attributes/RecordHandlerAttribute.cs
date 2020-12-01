using System;

namespace Bebop.Attributes
{
    /// <summary>
    /// Represents a class that will be used for handling bounded records
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RecordHandlerAttribute : Attribute
    {
    }
}