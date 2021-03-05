#!/usr/bin/env bash

echo "Compiling schema..."
dotnet run --project ../../../Compiler --files union1.bop --cs union1.cs --ts union1.ts --cpp union1.hpp
echo "Running npm install..."
npm install

echo "Testing that all implementations agree on encoding..."
dotnet run encode > cs.enc
npx ts-node encode.ts > ts.enc
(g++ --std=c++17 encode.cpp && ./a.out) > cpp.enc
rm -f ./a.out

failed=0
fail() { echo -e "\033[1;31m$1\033[0m"; failed=1; }

cmp cs.enc ts.enc || fail "C# and TypeScript encodes differ."
cmp cs.enc cpp.enc || fail "C# and C++ encodes differ."

echo "Testing that all implementations can decode the buffer agreed on..."
dotnet run decode cs.enc || fail "C# decode failed."
npx ts-node decode.ts cs.enc || fail "TypeScript decode failed."
(g++ --std=c++17 decode.cpp && ./a.out cs.enc) || fail "C++ decode failed."
rm -f ./a.out

if [ "$failed" = "0" ]; then
  echo -e "\033[32mAll tests passed.\033[0m"
fi
