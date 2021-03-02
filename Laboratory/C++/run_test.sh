#!/usr/bin/env bash
set -e
>&2 echo "Timing bebopc:"
time dotnet run --project ../../Compiler --cpp "gen/$1.hpp" --files "../Schemas/$1.bop"
>&2 echo "Timing C++ compiler:"
time g++ -std=c++17 test/$1.cpp
./a.out
