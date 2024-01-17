#!/bin/sh

outDir="build"
outFile="${outDir}/main.wasm"
inputFile="chord/main.go"

mkdir -p $outDir
tinygo build -o $outFile -gc=leaking -scheduler=none -target=wasi $inputFile
wasm2wat $outFile >"${outDir}/main.wat"
