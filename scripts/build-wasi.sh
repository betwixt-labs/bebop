#!/bin/sh

set -e

export WASI_VERSION=20
export WASI_VERSION_FULL=${WASI_VERSION}.0
compiler_dir="$(readlink -f ../compiler)"
export WASI_SDK_PATH="$(readlink -f ~/.wasi-sdk/wasi-sdk-${WASI_VERSION_FULL})"

echo "Building WASI.."
echo "WASI SDK path: $WASI_SDK_PATH"
dotnet publish "$compiler_dir" -c Release \
    /p:RuntimeIdentifier=wasi-wasm \
    /p:PublishSingleFile=false \
    /p:WasmSingleFileBundle=true \
    /p:WASI_SDK_PATH="$WASI_SDK_PATH" \
    /p:InvariantGlobalization=true \
    /p:TrimMode=full \
    /p:DebuggerSupport=false \
    /p:EventSourceSupport=false \
    /p:StackTraceSupport=false \
    /p:UseSystemResourceKeys=true \
    /p:NativeDebugSymbols=false \
