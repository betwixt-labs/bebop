using System;
using JetBrains.Annotations;

namespace Bebop.Attributes
{
    /// <summary>
    /// Represents a class that will be used for handling bounded records
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MeansImplicitUse]
    public class RecordHandlerAttribute : Attribute
    {
    }
}