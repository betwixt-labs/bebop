use bebop_tools as bebop;
use std::path::PathBuf;

use protobuf_codegen_pure as proto;

#[cfg(windows)]
const BEBOP_BIN: &str = "../../../bin/compiler/Debug/artifacts/bebopc.exe";
#[cfg(unix)]
const BEBOP_BIN: &str = "../../../bin/compiler/Debug/artifacts/bebopc";

fn main() {
    unsafe {
        bebop::COMPILER_PATH = Some(PathBuf::from(BEBOP_BIN));
    }
    bebop::build_schema_dir("schemas", "src/bebops", &Default::default());

    proto::Codegen::new()
        .out_dir("src/protos")
        .inputs(&["schemas/jazz.proto"])
        .include("schemas")
        .run()
        .expect("Codegen failed");
}
