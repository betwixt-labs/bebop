#!/usr/bin/env bash
set -e
>&2 echo "Timing bebopc:"

if [ -e /proc/version ] && grep -q Microsoft /proc/version; then
  # Windows: Visual Studio + WSL to run this script
  bebopc="../../bin/compiler/Windows-Debug/bebopc.exe"
else
  # Linux or Mac
  bebopc="dotnet run --project ../../Compiler"
fi
$bebopc --cpp "gen/$1.hpp" --files "../Schemas/Valid/$1.bop"
>&2 echo "Timing C++ compiler:"
time g++ -std=c++17 test/$1.cpp
./a.out
