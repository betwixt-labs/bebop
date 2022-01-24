use std::collections::LinkedList;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;

#[cfg(feature = "downloader")]
pub use downloader::*;
#[cfg(feature = "format")]
use format::*;

/// Configurable compiler path. By default it will use the downloaded executeable or assume it is in PATH.
pub static mut COMPILER_PATH: Option<PathBuf> = None;
pub static mut GENERATED_PREFIX: Option<String> = None;

#[cfg(feature = "downloader")]
mod downloader;
#[cfg(feature = "format")]
mod format;

#[derive(Debug)]
pub struct BuildConfig {
    /// Whether to skip generating the autogen module doc header in the files. Default: `false`.
    pub skip_generated_notice: bool,
    /// Whether to generate a `mod.rs` file of all the source. Default: `true`.
    pub generate_module_file: bool,
    /// Whether files should be automatically formatted after being generated.
    /// Does nothing if feature `format` is not enabled. Default: `true`.
    pub format_files: bool,
}

impl Default for BuildConfig {
    fn default() -> Self {
        Self {
            skip_generated_notice: false,
            generate_module_file: true,
            format_files: true,
        }
    }
}

/// Build all schemas in a given directory and write them to the destination directory including a
/// `mod.rs` file.
///
/// **WARNING: THIS DELETES DATA IN THE DESTINATION DIRECTORY** use something like `src/bebop` or `src/generated`.
pub fn build_schema_dir(
    source: impl AsRef<Path>,
    destination: impl AsRef<Path>,
    config: &BuildConfig,
) {
    if !destination.as_ref().exists() {
        fs::create_dir_all(destination.as_ref()).unwrap();
    }

    // clean all previously built files
    fs::read_dir(destination.as_ref())
        .unwrap()
        .filter_map(|entry| {
            let entry = entry.unwrap();
            let name = entry.file_name().to_str().unwrap().to_string();
            if entry.file_type().unwrap().is_file() && name != "mod.rs" {
                Some(name)
            } else {
                None
            }
        })
        .for_each(|file| fs::remove_file(PathBuf::from(destination.as_ref()).join(file)).unwrap());

    // build all files and update lib.rs
    let files = recurse_schema_dir(source, destination.as_ref(), config);

    // update the mod file
    if config.generate_module_file {
        let mod_file_path = PathBuf::from(destination.as_ref()).join("mod.rs");
        fs::write(
            &mod_file_path,
            &files
                .into_iter()
                .map(|mut schema_name| {
                    schema_name.insert_str(0, "pub mod ");
                    schema_name.push(';');
                    schema_name.push('\n');
                    schema_name
                })
                .collect::<String>(),
        )
        .unwrap();

        #[cfg(feature = "format")]
        if config.format_files {
            fmt_file(mod_file_path);
        }
    }
}

/// Build a single schema file and write it to the destination file.
///
/// **WARNING: THIS OVERWRITES THE DESTINATION FILE.**
pub fn build_schema(schema: impl AsRef<Path>, destination: impl AsRef<Path>, config: &BuildConfig) {
    let (schema, destination) = (schema.as_ref(), destination.as_ref());
    let compiler_path = compiler_path();
    println!("cargo:rerun-if-changed={}", compiler_path.to_str().unwrap());
    println!("cargo:rerun-if-changed={}", schema.to_str().unwrap());

    let mut cmd = Command::new(compiler_path);
    if config.skip_generated_notice {
        cmd.arg("--skip-generated-notice");
    }
    let output = cmd
        .arg("--files")
        .arg(schema)
        .arg("--rust")
        .arg(destination.to_str().unwrap())
        .output()
        .expect("Could not run bebopc");

    if !(output.status.success()) {
        println!(
            "cargo:warning=Failed to build schema {}",
            schema.to_str().unwrap()
        );
        for line in String::from_utf8(output.stdout).unwrap().lines() {
            println!("cargo:warning=STDOUT: {}", line);
        }
        for line in String::from_utf8(output.stderr).unwrap().lines() {
            println!("cargo:warning=STDERR: {}", line);
        }
        panic!("Failed to build schema!");
    }

    #[cfg(feature = "format")]
    if config.format_files {
        fmt_file(destination);
    }
}

fn recurse_schema_dir(
    dir: impl AsRef<Path>,
    dest: impl AsRef<Path>,
    config: &BuildConfig,
) -> LinkedList<String> {
    let mut list = LinkedList::new();
    for dir_entry in fs::read_dir(&dir).unwrap() {
        let dir_entry = dir_entry.unwrap();
        let file_type = dir_entry.file_type().unwrap();
        let file_path = PathBuf::from(dir.as_ref()).join(dir_entry.file_name());
        if file_type.is_dir() {
            if dir_entry.file_name() == "ShouldFail" {
                // do nothing
            } else {
                list.append(&mut recurse_schema_dir(&file_path, dest.as_ref(), config));
            }
        } else if file_type.is_file()
            && file_path
                .extension()
                .map(|s| s.to_str().unwrap())
                .unwrap_or("")
                == "bop"
        {
            let fname = format!(
                "{}{}",
                unsafe { GENERATED_PREFIX.as_deref().unwrap_or_else(|| "".into()) },
                file_stem(file_path.as_path())
            );
            build_schema(
                canonicalize(file_path.to_str().unwrap()),
                canonicalize(&dest).join(fname.clone() + ".rs"),
                config,
            );
            list.push_back(fname);
        } else {
            // do nothing
        }
    }
    list
}

fn file_stem(path: impl AsRef<Path>) -> String {
    path.as_ref()
        .file_stem()
        .unwrap()
        .to_str()
        .unwrap()
        .to_string()
}

fn canonicalize(path: impl AsRef<Path>) -> PathBuf {
    let p = path
        .as_ref()
        .canonicalize()
        .unwrap()
        .to_str()
        .unwrap()
        .to_string();
    if p.starts_with(r"\\?\") {
        p.strip_prefix(r"\\?\").unwrap()
    } else {
        &p
    }
    .into()
}

fn compiler_path() -> PathBuf {
    (unsafe { COMPILER_PATH.clone() }).unwrap_or_else(|| canonicalize("bebopc"))
}
