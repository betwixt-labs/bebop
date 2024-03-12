#!/usr/bin/env bash
mkdir -p gen
dotnet run --project ../../Compiler --trace -i ../Schemas/Valid/{array_of_strings,const,jazz,request}.bop build -g dart:gen/gen.dart
pub run test
