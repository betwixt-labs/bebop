using System;
using Core.Meta;

namespace Core.Generators
{
    public abstract class BaseGenerator
    {


        /// <summary>
        /// The schema to generate code from.
        /// </summary>
        protected BebopSchema Schema;

        protected BaseGenerator(BebopSchema schema)
        {
            Schema = schema;
        }

        /// <summary>
        /// Generate code for a Bebop schema.
        /// </summary>
        /// <param name="languageVersion">Determines a default language version the generated code will target.</param>
        /// <param name="services">Determines which components of a service will be generated. default to both client and server.</param>
        /// <param name="writeGeneratedNotice">Whether a generation notice should be written at the top of files. This is true by default.</param>
        /// <param name="emitBinarySchema">Whether a binary schema should be emitted. This is false by default.</param>
        /// <returns>The generated code.</returns>
        public abstract string Compile(Version? languageVersion, TempoServices services = TempoServices.Both, bool writeGeneratedNotice = true, bool emitBinarySchema = false);

        /// <summary>
        /// Write auxiliary files to an output directory path.
        /// </summary>
        /// <param name="outputPath">The output directory path.</param>
        public abstract void WriteAuxiliaryFiles(string outputPath);

        /// <summary>
        /// Get auxiliary file contents that should be written to disk.
        /// </summary>
        public abstract AuxiliaryFile? GetAuxiliaryFile();
        /// <summary>
        /// Get the alias of the generator.
        /// </summary>
        public abstract string Alias { get; }
    }
}