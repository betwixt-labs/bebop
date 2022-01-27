use std::path::{Path, PathBuf};
use std::process::Command;

use which::which;

/// Get the rustfmt binary path.
fn rustfmt_path() -> &'static Path {
    // lazy static var
    static mut PATH: Option<PathBuf> = None;

    if let Some(path) = unsafe { PATH.as_ref() } {
        return path;
    }

    if let Ok(path) = std::env::var("RUSTFMT") {
        unsafe {
            PATH = Some(PathBuf::from(path));
            PATH.as_ref().unwrap()
        }
    } else {
        // assume it is in PATH
        unsafe {
            PATH = Some(which("rustmft").unwrap_or_else(|_| "rustfmt".into()));
            PATH.as_ref().unwrap()
        }
    }
}

pub fn fmt_file(path: impl AsRef<Path>) {
    let pathstr = path.as_ref().to_str().unwrap();
    if let Err(err) = Command::new(rustfmt_path()).arg(pathstr).output() {
        println!(
            "cargo:warning=Failed to run rustfmt for {:?}, ignoring ({})",
            path.as_ref(),
            err
        );
    }
}
