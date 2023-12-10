using System.CommandLine;

namespace Compiler.Options
{
    /// <summary>
    /// Represents a command line option for including certain schemas.
    /// </summary>
    public class IncludeOption : CliOption<string[]>
    {
        public IncludeOption() : base(
            name: "--include",
            aliases: new[] { "-i" })
        {
            Description = "Includes certain schemas. Supports globbing.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for excluding certain schemas.
    /// </summary>
    public class ExcludeOption : CliOption<string[]>
    {
        public ExcludeOption() : base(
            name: "--exclude",
            aliases: new[] { "-e" })
        {
            Description = "Excludes certain schemas. Supports globbing.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for excluding certain directories from being watched.
    /// </summary>
    public class WatchExcludeDirectoriesOption : CliOption<string[]>
    {
        public WatchExcludeDirectoriesOption() : base(
            name: "--exclude-directories",
            aliases: new[] { "-ed" })
        {
            Description = "Excludes certain directories from being watched. Supports globbing.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for excluding certain files from being watched.
    /// </summary>
    public class WatchExcludeFilesOption : CliOption<string[]>
    {
        public WatchExcludeFilesOption() : base(
            name: "--exclude-files",
            aliases: new[] { "-ef" })
        {
            Description = "Excludes certain files from being watched. Supports globbing.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for disabling the emission of files.
    /// </summary>
    public class NoEmitOption : CliOption<bool>
    {
        public NoEmitOption() : base(
            name: "--no-emit",
            aliases: new[] { "-ne" })
        {
            Description = "Disables emitting files.";
            AllowMultipleArgumentsPerToken = false;
        }
    }

    /// <summary>
    /// Represents a command line option for suppressing specific warnings.
    /// </summary>
    public class NoWarnOption : CliOption<int[]>
    {
        public NoWarnOption() : base(
            name: "--no-warn",
            aliases: new[] { "-nw" })
        {
            Description = "Suppresses the specified warning codes.";
            AllowMultipleArgumentsPerToken = true;
        }
    }

    /// <summary>
    /// Represents a command line option for specifying the namespace.
    /// </summary>
    public class NamespaceOption : CliOption<string>
    {
        public NamespaceOption() : base(
            name: "--namespace",
            aliases: new[] { "-ns" })
        {
            Description = "The namespace to use for the generated code.";
            AllowMultipleArgumentsPerToken = false;
        }
    }
}