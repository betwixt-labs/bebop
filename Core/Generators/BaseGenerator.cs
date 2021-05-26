using System;
using Core.Meta.Interfaces;

namespace Core.Generators
{
    public abstract class BaseGenerator
    {
        /// <summary>
        /// The schema to generate code from.
        /// </summary>
        protected ISchema Schema;


        protected BaseGenerator(ISchema schema)
        {
            Schema = schema;
        }

        /// <summary>
        /// Generate code for a Bebop schema.
        /// </summary>
        /// <param name="languageVersion">Determines a default language version the generated code will target.</param>
        /// <returns>The generated code.</returns>
        public abstract string Compile(Version? languageVersion);

        /// <summary>
        /// Write auxiliary files to an output directory path.
        /// </summary>
        /// <param name="outputPath">The output directory path.</param>
        public abstract void WriteAuxiliaryFiles(string outputPath);
    }
}