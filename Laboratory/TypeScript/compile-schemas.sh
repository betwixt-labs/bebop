#!/bin/bash

for path in ../Schemas/*.bop; do
  filename=${path##*/};
  dotnet run --project ../../Compiler --lang ts --out "test/generated/${filename%.*}.ts" --files "$path"
done

