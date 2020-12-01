namespace Repl
{
    /// <summary>
    /// Holds results from the Bebop compiler
    /// </summary>
    /// <remarks>
    /// Blazor doesn't support returning ValueTuples to Javascript so here we are.
    /// </remarks>
    public class CompilerOutput
    {
        /// <summary>
        /// Indicates whether the compiler succeeded or not.
        /// </summary>
        public bool IsOk { get; set; }
        /// <summary>
        /// The returned value from the compiler. Either a stacktrace or generated code.
        /// </summary>
        public string Result { get; set; }
    }
}
