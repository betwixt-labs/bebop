#!/bin/bash

for path in ../Schemas/*.bop; do
  filename=${path##*/};
  dotnet run --project ../../Compiler --ts "test/generated/${filename%.*}.ts" --files "$path"
done

