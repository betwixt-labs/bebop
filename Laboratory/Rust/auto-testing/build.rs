use bebop_tools as bebop;
use bebop_tools::BuildConfig;
use std::path::PathBuf;

#[cfg(windows)]
const BEBOP_BIN: &str = "../../../bin/compiler/Windows-Debug/bebopc.exe";
#[cfg(unix)]
const BEBOP_BIN: &str = "../../../bin/compiler/Linux-Debug/bebopc";

fn main() {
    unsafe {
        bebop::COMPILER_PATH = Some(PathBuf::from(BEBOP_BIN));
        bebop::GENERATED_PREFIX = Some("_".into());
    }
    bebop::build_schema_dir(
        "../../Schemas",
        "src/generated",
        &BuildConfig {
            skip_generated_notice: true,
            ..Default::default()
        },
    );
}
