#!/usr/bin/env bash
mkdir -p gen
dotnet run --project ../../Compiler --files ../Schemas/{array_of_strings,const,jazz,request}.bop --dart gen/gen.dart
pub run test
