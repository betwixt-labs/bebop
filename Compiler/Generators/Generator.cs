using Compiler.Meta.Interfaces;

namespace Compiler.Generators
{
    public abstract class Generator
    {
        /// <summary>
        /// The schema to generate code from.
        /// </summary>
        protected ISchema _schema;

        protected Generator(ISchema schema)
        {
            _schema = schema;
        }

        /// <summary>
        /// Generate code for a Pierogi schema.
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