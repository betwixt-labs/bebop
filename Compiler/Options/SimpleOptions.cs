using System.CommandLine;

namespace Compiler.Options
{
    /// <summary>
    /// Represents a command line option for including certain schemas.
    /// </summary>
    public class IncludeOption : CliOption<string[]>
    {
        public IncludeOption() : base(
            name: CliStrings.IncludeFlag,
            aliases: ["-i"])
        {
            Description = "Specifies an array of filenames or patterns to include in the compiler. These filenames are resolved relative to the directory containing the bebop.json file.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for excluding certain schemas.
    /// </summary>
    public class ExcludeOption : CliOption<string[]>
    {
        public ExcludeOption() : base(
            name: CliStrings.ExcludeFlag,
            aliases: ["-e"])
        {
            Description = "Specifies an array of filenames or patterns that should be skipped when resolving include.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for excluding certain directories from being watched.
    /// </summary>
    public class WatchExcludeDirectoriesOption : CliOption<string[]>
    {
        public WatchExcludeDirectoriesOption() : base(
            name: CliStrings.ExcludeDirectoriesFlag)
        {
            Description = "Remove a list of directories from the watch process.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for excluding certain files from being watched.
    /// </summary>
    public class WatchExcludeFilesOption : CliOption<string[]>
    {   
        public WatchExcludeFilesOption() : base(
            name: CliStrings.ExcludeFilesFlag)
        {
            Description = "Remove a list of files from the watch mode's processing.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    public class PreserveWatchOutputOption : CliOption<bool>
    {
        public PreserveWatchOutputOption() : base(
            name: CliStrings.PreserveWatchOutputFlag)
        {
            Description = "Disable wiping the console in watch mode.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    /// <summary>
    /// Represents a command line option for disabling the emission of files.
    /// </summary>
    public class NoEmitOption : CliOption<bool>
    {
        public NoEmitOption() : base(
            name: CliStrings.NoEmitFlag)
        {
            Description = "Disable emitting files from a compilation.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    /// <summary>
    /// Represents a command line option for suppressing specific warnings.
    /// </summary>
    public class NoWarnOption : CliOption<int[]>
    {
        public NoWarnOption() : base(
            name: CliStrings.NoWarnFlag)
        {
            Description = "Suppresses the specified warning codes from reports during compilation.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    public class InitOption : CliOption<bool>
    {
        public InitOption() : base(
            name: CliStrings.InitFlag)
        {
            Description = "Initializes a Bebop project and creates a bebop.json file.";
            AllowMultipleArgumentsPerToken = false;
        }
    }
    
    public class ListSchemaOption : CliOption<bool>
    {
        public ListSchemaOption() : base(
            name: CliStrings.ListSchemasFlag)
        {
            Description = "Print names of schemas that are part of the compilation and then stop processing.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    public class ShowConfigOption : CliOption<bool>
    {
        public ShowConfigOption() : base(
            name: CliStrings.ShowConfigFlag)
        {
            Description = "Print the final configuration instead of building.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    public class LocaleOption : CliOption<string>
    {
        public LocaleOption() : base(
            name: CliStrings.LocaleFlag)
        {
            Description = "Set the language of the messaging from bebopc. This does not affect emit.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    public class TraceOption : CliOption<bool>
    {
        public TraceOption() : base(
            name: CliStrings.TraceFlag)
        {
            Description = "Enable tracing of the compiler.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    public class StandardInputOption : CliOption<bool>
    {
        public StandardInputOption() : base(
            name: CliStrings.StandardInputFlag)
        {
            Description = "Read a schema from standard input.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    public class StandardOutputOption : CliOption<bool>
    {
        public StandardOutputOption() : base(
            name: CliStrings.StandardOutputFlag)
        {
            Description = "Write the results of compilation to standard output.";
            AllowMultipleArgumentsPerToken = false;
        }
    }
}