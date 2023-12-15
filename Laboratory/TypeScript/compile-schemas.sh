#!/bin/bash

dotnet run --project ../../Compiler --trace --include ../Schemas/Valid/*.bop build --generator "ts:test/generated/gen.ts"