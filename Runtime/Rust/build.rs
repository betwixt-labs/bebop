use std::fs;
use std::fs::File;
use std::io::{Read, Write};
use std::path::{Path, PathBuf};

use bebop_tools as bebop;
use bebop_tools::BuildConfig;

#[cfg(windows)]
const BEBOP_BIN: &str = "../../bin/compiler/Windows-Debug/bebopc.exe";
#[cfg(unix)]
const BEBOP_BIN: &str = "../../bin/compiler/Linux-Debug/bebopc";

const RPC_DATAGRAM_SCHEMA: &str = "../../Core/Schemas/RpcDatagram.bop";
const RPC_DATAGRAM_OUT: &str = "src/rpc/datagram.rs";
const RPC_DATAGRAM_TMP: &str = "src/rpc/datagram.rs.tmp";

fn main() {
    #[cfg(feature = "rpc")]
    {
        unsafe {
            bebop::COMPILER_PATH = Some(PathBuf::from(BEBOP_BIN));
            bebop::GENERATED_PREFIX = Some("_".into());
        }
        let cfg = BuildConfig {
            feature_flag: Some("rpc-datagram".into()),
            format_files: false,
            ..Default::default()
        };
        bebop::build_schema(RPC_DATAGRAM_SCHEMA, RPC_DATAGRAM_TMP, &cfg);

        let mut dg = String::new();
        File::open(RPC_DATAGRAM_TMP)
            .unwrap()
            .read_to_string(&mut dg)
            .unwrap();
        let dg = dg.replace("::bebop", "crate");
        if Path::new(RPC_DATAGRAM_OUT).exists() {
            fs::remove_file(RPC_DATAGRAM_OUT).unwrap();
        }
        File::create(RPC_DATAGRAM_OUT)
            .unwrap()
            .write_all(dg.as_bytes())
            .unwrap();
        fs::remove_file(RPC_DATAGRAM_TMP).unwrap();
        bebop::fmt_file(RPC_DATAGRAM_OUT);
    }
}
