# Bebop

Bebop is a schema-based binary serialization technology, similar to Protocol Buffers or MessagePack. In particular, Bebop tries to be a good fit for client–server or distributed web apps that need something faster, more concise, and more type-safe than JSON or MessagePack, while also avoiding some of the complexity of Protocol Buffers, FlatBuffers and the like.

![Bebop logo; The word Bebop, but the second B is replaced with a saxophone with ones and zeros coming out of it.](./assets/128@2x.png#gh-light-mode-only)![Bebop logo; The word Bebop, but the second B is replaced with a saxophone with ones and zeros coming out of it.](./assets/dark-mode/128@2x.png#gh-dark-mode-only)

[![Compiler Build](https://img.shields.io/github/workflow/status/RainwayApp/bebop/build-bebopc?label=Compiler%20Build)](https://github.com/RainwayApp/bebop/actions/workflows/build-bebopc.yml)
[![REPL Build](https://img.shields.io/github/workflow/status/RainwayApp/bebop/Bebop%20WebAssembly%20REPL?label=REPL%20Build)](https://github.com/RainwayApp/bebop/actions/workflows/build-repl.yml)
[![Integration Tests](https://img.shields.io/github/workflow/status/RainwayApp/bebop/Integration%20Tests?label=Integration%20Tests)](https://github.com/RainwayApp/bebop/actions/workflows/integration-tests.yml)

[![Test .NET](https://img.shields.io/github/workflow/status/RainwayApp/bebop/Bebop%20.NET%20Runtime?label=Test%20.NET)](https://github.com/RainwayApp/bebop/actions/workflows/build-runtime-cs.yml)
[![Test Rust](https://img.shields.io/github/workflow/status/RainwayApp/bebop/Test%20Rust?label=Test%20Rust)](https://github.com/RainwayApp/bebop/actions/workflows/test-rust.yml)
[![Test TypeScript](https://img.shields.io/github/workflow/status/RainwayApp/bebop/Test%20TypeScript?label=Test%20TypeScript)](https://github.com/RainwayApp/bebop/actions/workflows/test-typescript.yml)
[![Test Dart](https://img.shields.io/github/workflow/status/RainwayApp/bebop/Test%20Dart?label=Test%20Dart)](https://github.com/RainwayApp/bebop/actions/workflows/test-dart.yml)

Bebop is fast! Read the [initial release blog](https://rainway.com/blog/2020/12/09/bebop-an-efficient-schema-based-binary-serialization-format/) for benchmarks and more info.

## Releases
To find the latest release of the Bebop compiler and its corresponding runtimes, visit the [release page](https://github.com/RainwayApp/bebop/releases).

## Documentation
Bebop is documented on [this repository's wiki](https://github.com/RainwayApp/bebop/wiki). Here are some quick links to get you started:

- [Writing Bops: The Bebop Schema Language](https://github.com/RainwayApp/bebop/wiki/Writing-Bops:-The-Bebop-Schema-Language)
- [Getting Started with .NET](https://github.com/RainwayApp/bebop/wiki/Getting-Started-with-.NET)
- [Getting Started with TypeScript](https://github.com/RainwayApp/bebop/wiki/Getting-Started-with-TypeScript)

## Web REPL
If you want to get familiar with the schema language and see what the generated code looks like, try out the [web REPL](https://bebop.sh/repl/).

[_See You Space Cowboy_...](https://www.youtube.com/watch?v=u1UZHXB_r6g)
