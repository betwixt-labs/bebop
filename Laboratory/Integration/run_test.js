const shell = require("shelljs");
const cxx = shell.env["CXX"] || "g++";
const aout = process.platform === "win32" ? "a.exe" : "./a.out";

shell.echo("Compiling schema...");
if (shell.exec("dotnet run --project ../../Compiler --files schema.bop --cs schema.cs --ts schema.ts --cpp schema.hpp --rust Rust/src/schema.rs").code !== 0) {
  shell.echo("Error: bebopc failed");
  shell.exit(1);
}

shell.echo("Generating encodings from each language...")
shell.exec("dotnet run encode > cs.enc");
shell.exec("npx ts-node encode.ts > ts.enc");
if (shell.exec(`${cxx} --std=c++17 encode.cpp`).code !== 0) {
  shell.echo(`Error: ${cxx} encode.cpp failed`);
  shell.exit(1);
}
shell.exec(`${aout} > cpp.enc`);
shell.rm("-f", aout);
shell.exec("cargo run --manifest-path Rust/Cargo.toml --example encode > rs.enc");

// Files can have some variance and still be equivalent because of ordering in maps and C# dates.
// Perform full matrix testing because it seems like a good idea

shell.echo("Testing that all implementations can decode all encodings...");
if (shell.exec(`${cxx} --std=c++17 decode.cpp`).code !== 0) {
  shell.echo(`Error: ${cxx} decode.cpp failed`);
  shell.exit(1);
}

// rust first because it has the most useful errors
const languages = [
  {name: "Rust", cmd: "cargo run --manifest-path Rust/Cargo.toml -q --example decode --", file: "rs.enc"},
  {name: "C#", cmd: "dotnet run decode", file: "cs.enc"},
  {name: "TypeScript", cmd: "npx ts-node decode.ts", file: "ts.enc"},
  {name: "C++", cmd: aout, file: "cpp.enc"},
];

var failed = false;
for (const decode of languages) {
  for (const encode of languages) {
    shell.echo(`\x1b[33m${decode.cmd} ${encode.file}\x1b[0m`);
    if (shell.exec(`${decode.cmd} ${encode.file}`).code !== 0) {
      shell.echo(`\x1b[31mError: ${decode.name} failed to decode ${encode.name}\x1b[0m`);
      failed = true;
    }
  }
}

shell.rm("-f", aout);

if (failed) {
  shell.exit(1);
} else {
  shell.echo("\x1b[32mAll tests passed.\x1b[0m")
}
