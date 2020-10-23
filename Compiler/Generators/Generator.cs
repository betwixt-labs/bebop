using Compiler.Meta.Interfaces;

namespace Compiler.Generators
{
    public abstract class Generator
    {
        /// <summary>
        /// The schema to generate code from.
        /// </summary>
        protected ISchema Schema;

        protected Generator(ISchema schema)
        {
            Schema = schema;
        }

        /// <summary>
        /// Generate code for a Bebop schema.
        /// </summary>
        /// <returns>The generated code.</returns>
        public abstract string Compile();

        /// <summary>
        /// Write auxiliary files to an output directory path.
        /// </summary>
        /// <param name="outputPath">The output directory path.</param>
        public abstract void WriteAuxiliaryFiles(string outputPath);
    }
}