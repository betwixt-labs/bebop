---
title: What is a chord.json
---

Similar to `package.json` in JavaScript projects, `chord.json` is the manifest file for Chord extensions.

It contains metadata about the extension, such as its name, version, and instructions on how to build it.

It also defines contributions the extension makes, such as the generator and any decorators it provides.

## Example

```json
{
  "name": "typescript-template",
  "private": true,
  "description": "A template for chords built with typescript",
  "version": "1.0.0",
  "repository": "https://github.com/betwixt-labs/bebopc",
  "license": "Apache-2.0",
  "author": {
    "name": "Betwixt Labs",
    "email": "code@betwixtlabs.com"
  },
  "bin": "dist/index.wasm",
  "build": {
    "script": "yarn build",
    "compiler": "javy"
  },
  "contributes": {
    "generator": {
      "alias": "acme",
      "name": "ACME Generator"
    },
    "decorators": {
      "example": {
        "description": "This decorator acts as an example for how to define a decorator",
        "parameters": {
          "floor": {
            "description": "Specifies the floor of something idk",
            "type": "int32",
            "required": true
          },
          "ceiling": {
            "description": "Specifies the ceiling of something idk",
            "type": "int32",
            "required": false,
            "default": 100
          }
        },
        "targets": "all"
      }
    }
  },
  "engine": {
    "bebopc": "^3.0.0"
  },
  "readme": "README.md"
}
```
