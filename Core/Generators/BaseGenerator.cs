using System;
using System.Text.Json;
using Core.Meta;

namespace Core.Generators
{
    /// <summary>
    /// Represents an abstract base class for generating code from Bebop schemas. 
    /// This class encapsulates the common functionalities needed for various code generators.
    /// </summary>
    public abstract class BaseGenerator
    {
        /// <summary>
        /// The Bebop schema from which the code is generated.
        /// </summary>
        protected BebopSchema Schema = default!;

        /// <summary>
        /// Configuration settings specific to the generator.
        /// </summary>
        protected GeneratorConfig Config = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGenerator"/> class with a given schema and configuration.
        /// </summary>
        /// <param name="schema">The Bebop schema used for code generation.</param>
        /// <param name="config">The generator-specific configuration settings.</param>
        protected BaseGenerator() { }

        /// <summary>
        /// Generates code based on the provided Bebop schema.
        /// </summary>
        /// <returns>A string containing the generated code.</returns>
        public abstract string Compile(BebopSchema schema, GeneratorConfig config);

        /// <summary>
        /// Writes auxiliary files, if any, associated with the generated code to the specified output directory.
        /// </summary>
        /// <param name="outputPath">The directory path where auxiliary files should be written.</param>
        public abstract void WriteAuxiliaryFile(string outputPath);

        /// <summary>
        /// Retrieves information about any auxiliary files associated with the generated code.
        /// </summary>
        /// <returns>An <see cref="AuxiliaryFile"/> representing the contents and metadata of the auxiliary file, or null if there are no auxiliary files.</returns>
        public abstract AuxiliaryFile? GetAuxiliaryFile();

        /// <summary>
        /// Gets the alias of the code generator, which uniquely identifies it among other generators.
        /// </summary>
        public abstract string Alias { get; set; }

        /// <summary>
        /// Gets the friendly name of the code generator, which is used for display purposes.
        /// </summary>
        public abstract string Name { get; set; }
    }
}
