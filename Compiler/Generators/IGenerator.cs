using Compiler.Meta.Interfaces;

namespace Compiler.Generators
{
    public interface IGenerator
    {
        /// <summary>
        /// Generate code for a Pierogi schema.
        /// </summary>
        /// <returns>The generated code.</returns>
        string Compile();

        /// <summary>
        /// Write auxiliary files to the output path.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        void WriteAuxiliaryFiles(string outputPath);
    }
}