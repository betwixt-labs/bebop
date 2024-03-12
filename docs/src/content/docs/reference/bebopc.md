---
title: bebopc
---
```
Description:
  The Bebop schema language compiler

Usage:
  bebopc [command] [options]

Options:
  -?, -h, --help                                    Show help and usage information
  --version                                         Show version information
  -df, --diagnostic-format <Enhanced|JSON|MSBuild>  Specifies the format which diagnostic messages are printed in. [default: Enhanced]
  -c, --config                                      Compile the project given the path to its configuration file. [default: Core.Meta.BebopConfig]
  -i, --include                                     Specifies an array of filenames or patterns to include in the compiler. These filenames are resolved relative to the directory 
                                                    containing the bebop.json file.
  -e, --exclude                                     Specifies an array of filenames or patterns that should be skipped when resolving include.
  --init                                            Initializes a Bebop project and creates a bebop.json file.
  --list-schemas-only                               Print names of schemas that are part of the compilation and then stop processing.
  --show-config                                     Print the final configuration instead of building.
  --locale                                          Set the language of the messaging from bebopc. This does not affect emit.
  --trace                                           Enable tracing of the compiler.

Commands:
  build    Build schemas into one or more target languages.
  watch    Watch input files.
  convert  Convert schema files from one format to another.

```

To get help on a specific command, use `bebopc <command> --help`.