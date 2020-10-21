using Compiler.Meta.Interfaces;

namespace Compiler.Generators
{
    public interface IGenerator
    {
        /// <summary>
        /// Generate code for a given Pierogi schema.
        /// </summary>
        /// <param name="schema">A Pierogi schema.</param>
        /// <returns>The generated code.</returns>
        string Compile(ISchema schema);

        /// <summary>
        /// Write auxiliary files to the output path.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        void WriteAuxiliaryFiles(string outputPath);
    }
}