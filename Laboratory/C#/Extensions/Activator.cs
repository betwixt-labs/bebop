using Bebop.Extensions;

// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    /// Dirty hack that allows using a fast implementation
    /// of the activator.
    /// </summary>
    public static class Activator
    {
        public static T CreateInstance<T>() where T : new() => ActivatorImpl<T>.Create();

        private static class ActivatorImpl<T> where T : new()
        {
            public static readonly Func<T> Create =
                DynamicModuleLambdaCompiler.GenerateFactory<T>();
        }
    }
}