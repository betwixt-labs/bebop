#!/usr/bin/env zsh

echo "Compiling schema..."
dotnet run --project ../../Compiler --files schema.bop --cs schema.cs --ts schema.ts --cpp schema.hpp --rust Rust/src/schema.rs

echo "Running npm install..."
npm install

echo "Generating encodings from each language..."
dotnet run encode > cs.enc
npx ts-node encode.ts > ts.enc
(g++ --std=c++17 encode.cpp && ./a.out) > cpp.enc
rm -f ./a.out
cargo run --manifest-path Rust/Cargo.toml --example encode > rs.enc

failed=0
fail() { echo -e "\033[1;31m$1\033[0m"; failed=1; }

# Files can have some variance and still be equivalent because of ordering in maps and C# dates.
# Perform full matrix testing because it seems like a good idea

echo "Testing that all implementations can decode all encodings..."
# rust first because it has the most useful errors
cargo run --manifest-path Rust/Cargo.toml -q --example decode -- rs.enc || fail "Rust decode failed."
cargo run --manifest-path Rust/Cargo.toml -q --example decode -- cs.enc || fail "Rust failed to decode decode c#."
#cargo run --manifest-path Rust/Cargo.toml -q --example decode -- ts.enc || fail "Rust failed to decode decode ts."
cargo run --manifest-path Rust/Cargo.toml -q --example decode -- cpp.enc || fail "Rust failed to decode decode c++."

dotnet run decode cs.enc || fail "C# decode failed."
dotnet run decode rs.enc || fail "C# failed to decode rust."
#dotnet run decode ts.enc || fail "C# failed to decode ts."
dotnet run decode cpp.enc || fail "C# failed to decode c++."

npx ts-node decode.ts ts.enc || fail "TypeScript decode failed."
npx ts-node decode.ts rs.enc || fail "TypeScript failed to decode rust."
npx ts-node decode.ts cs.enc || fail "TypeScript failed to decode c#."
npx ts-node decode.ts cpp.enc || fail "TypeScript failed to decode c++."

g++ --std=c++17 decode.cpp
./a.out cpp.enc || fail "C++ decode failed"
./a.out rs.enc || fail "C++ failed to decode rust"
#./a.out ts.enc || fail "C++ failed to decode ts"
./a.out cs.enc || fail "C++ failed to decode c#"
rm -f ./a.out

if [ "$failed" = "0" ]; then
  echo -e "\033[32mAll tests passed.\033[0m"
else
  exit 1
fi
