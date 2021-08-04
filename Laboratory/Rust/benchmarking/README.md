# Bebop Rust Benchmarks

## Methodology

All tests are performed with criterion in groups. For each stage, the required data is prepared
before the test.

These tests are not comprehensive and may unjustly favor one format over another. Any benefits
granted to one format or another due to schema format was not our intent and is very hard to spot if
you are not intimately familiar with all of them. If you have a schema that you think would be a
good test case, please raise a PR; more tests will only help us better the bebop rust
implementation.

### Structure forms

All forms are created form the same core schema and are made to be as close to each other as
possible.

1. **native** (+ serde derive): This must be an owned structure format which is the "native" format
   that is used in the application. In reality there could be extra fields that serde can be
   configured to ignore or the message structs might be entirely separate.
2. **bebop**: This is a borrowed format, so we choose to borrow from the native format and implement
   a conversion through the `From` trait for each of the native types
   as `From<&'a Native> for Bebop<'a>`.
3. **protobuf**: This is an ugly structure form IMO which requires using getters and setters. It
   also uses some of its own wrappers such as `protobuf::Repeated`. It is unlikely this form would
   be used in an application even though it requires owned data.

### Stages

1. **structuring**: When you have a native format and need to make it conform to the serialization
   type. This cost is arguably avoided when you are not storing as a 1:1 native struct, such as if
   you construct the message on the fly and would have done that regardless. *We assume here that
   you do not need to restructure for serde but rather already use that form in your code. If that
   is not the case serde formats would have a slightly increased cost which we do not account for.*
2. **serialization**: Converting the schema structure into a linear binary array.
3. **deserialization**: Converting a linear binary array to the appropriate schema structure.
4. **destructuring**: When you read from the schema structure into a native format. This cost is
   often avoidable in the real-world if you only need to check certain fields or do not want/need to
   store it as an application-specific structure.

## Results

## Running the Tests

Main benchmarks can be run from this directory with
```shell
cargo bench
```
The results are then published to `../target/criterion`.

The flamegraphs can be generated using
```shell
cargo run --example flamegraph --release --features="pprof" -- --bench --profile-time 5
```
but will not work on Windows.
