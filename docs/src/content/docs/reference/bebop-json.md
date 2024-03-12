---
title: bebop.json
---

A `bebop.json` file in a directory indicates that the directory is the root of a Bebop project. 

This page covers all of the different options available inside a `bebop.json` file. There are five main sections:

- The [root fields](#root-fields) for letting the compiler know what files are available
- The [generators](#generators---generators) section for defining how to generate code
- The [watchOptions](#watch-options---watchoptions) fields, for tweaking the watch mode
- The [extensions](#extensions---extensions) section, for defining custom extensions

## Root Fields

### Include - `include`
Specifies an array of filenames or patterns to include in the compiler. These filenames are resolved relative to the directory containing the bebop.json file.

```json
{
  "include": [
    "src/**/*.bop"
  ]
}
```

Which would include all `.bop` files in the `src` directory and all subdirectories.

```
├── scripts                ⨯
│   ├── lint.ts            ⨯
│   ├── update_deps.ts     ⨯
│   └── utils.ts           ⨯
├── src                    ✓
│   ├── schemas            ✓
│   │    ├── client.bop    ✓
│   │    └── server.bop    ✓
├── tests                  ⨯
│   ├── dummy.bop          ⨯
```
`include` and `exclude` support wildcard characters to make glob patterns:
- `*` matches zero or more characters (excluding directory separators)
- `?` matches any one character (excluding directory separators)
- `**/` matches any directory nested to any level

If the last path segment in a pattern does not contain a file extension or wildcard character, then it is treated as a directory, and files with supported extensions inside that directory are included (e.g. `.bop`).

### Exclude - `exclude`

Specifies an array of filenames or patterns that should be skipped when resolving [include](#include---include).

:::note
`exclude` _only_ changes which files are included as a result of the include setting. A file specified by exclude can still become part of your codebase due to an import statement in your schema.
:::

### No Warn - `noWarn`
Specifies an array of warning codes to silence during compilation. 

```json
{
  "noWarn": [
    1000
  ]
}
```

### No Emit - `noEmit`
Disable emitting files from a compilation.

```json
{
  "noEmit": true
}
```


## Generators - `generators`

Specifies code generators to use for compilation.

Built-in generators alias are:
- `ts` - Generates TypeScript code
- `cs` - Generates C# code
- `cpp` - Generates C++ code
- `py` - Generates Python code
- `rust` - Generates Rust code
- `dart` - Generates Dart code



### Out File - `outFile`

Specifies the output file for the generated code. This field is **required** for all generators.

```json
{
  "generators": {
    "ts": {
      "outFile": "bops.gen.ts"
    }
  }
}
```

### Services - `services`

By default, the compiler generates a concrete client and a service base class. This property can be used to limit compiler asset generation.

Valid options are:
- `none` - Do not generate any service assets
- `client` - Generate a concrete client
- `server` - Generate a server base classes
- `both` - Generate both client and server assets

```json
{
  "generators": {
    "ts": {
      "services": "both"
    }
  }
}
```

### Emit Notice - `emitNotice`
Specify if the code generator should produce a notice stating code was auto-generated. (default `true`).

```json
{
  "generators": {
    "ts": {
      "emitNotice": false
    }
  }
}
```

### Emit Binary Schema - `emitBinarySchema`
Specify if the code generator should emit a binary schema in the output file that can be used for dynamic (de)serialization. (default `true`).

```json
{
  "generators": {
    "ts": {
      "emitBinarySchema": false
    }
  }
}
```

### Namespace - `namespace`

Specify a namespace for the generated code. Casing will be adjusted to match the target language.

```json
{
  "generators": {
    "ts": {
      "namespace": "MyNamespace"
    }
  }
}
```

### Options - `options`

Specify custom options for the code generator.

```json
{
  "generators": {
    "cs": {
      "options": {
        "languageVersion": "8.0"
      }
    }
  }
}
```

## Watch Options - `watchOptions`

Settings for the watch mode of bebopc.

### Exclude Files - `excludeFiles`

Remove a list of files from the watch mode's processing.

```json
{
  "watchOptions": {
    "excludeFiles": [
      "test/**/*.bop"
    ]
  }
}
```

### Exclude Directories - `excludeDirectories`
Remove a list of directories from the watch process.

```json
{
  "watchOptions": {
    "excludeDirectories": [
      "test"
    ]
  }
}
```


## Extensions - `extensions`
An object of extensions the compiler should load.

```json
{
  "extensions": {
    "custom-generator": "1.2.3"
  }
}
```


## Full Example

```json
{
  "include": [
    "src/**/*.bop"
  ],
  "exclude": [
    "src/**/test.bop"
  ],
  "generators": {
    "ts": {
      "outFile": "bops.gen.ts",
      "services": "both",
      "emitNotice": true,
      "emitBinarySchema": false
    }
  }
}
```