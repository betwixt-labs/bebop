#!/bin/bash

dotnet run --project ../../Compiler --ts "test/generated/gen.ts" --files ../Schemas/*.bop

