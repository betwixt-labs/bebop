use bebop_tools as bebop;
use std::path::PathBuf;

#[cfg(windows)]
const BEBOP_BIN: &str = "../../../bin/compiler/Windows-Debug/bebopc.exe";
#[cfg(unix)]
const BEBOP_BIN: &str = "../../../bin/compiler/Linux-Debug/bebopc";

fn main() {
    unsafe {
        bebop::COMPILER_PATH = Some(PathBuf::from(BEBOP_BIN));
    }
    // bebop::download_bebopc(
    //     PathBuf::from("../target").join("bebopc"),
    // );
    bebop::build_schema_dir("schemas", "src/generated");
}
