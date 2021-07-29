Projects here are designed to test the code generation and not the runtime itself. For Runtime tests
checkout the Runtime/Rust directory.

The manual project is for testing a manually generated schema and may be outdated.

Auto testing will generate schemas for all schemas in the schema directory and verify they compile,
but not validate their functionality. Run with `cargo check -p auto-testing`.

The functionality-testing project includes some hand-written tests to make sure the code that is
being generated works for a few specific schemas. Run with `cargo test -p functionality-testing`.

