use std::collections::LinkedList;
use std::path::{Path, PathBuf};
use std::process::Command;
use std::fs;

/// Configurable compiler path. By default it will use the downloaded executeable or assume it is in PATH.
pub static mut COMPILER_PATH: Option<PathBuf> = None;
pub static mut GENERATED_PREFIX: Option<String> = None;

#[cfg(feature = "downloader")]
mod downloader;
#[cfg(feature = "downloader")]
pub use downloader::*;

/// Build all schemas in a given directory and write them to the destination directory including a
/// `mod.rs` file.
///
/// **WARNING: THIS DELETES DATA IN THE DESTINATION DIRECTORY** use something like `src/bebop` or `src/generated`.
pub fn build_schema_dir(source: impl AsRef<Path>, destination: impl AsRef<Path>) {
    println!(
        "cargo:rerun-if-changed={}/mod.rs",
        destination.as_ref().to_str().unwrap()
    );

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
    let files = recurse_schema_dir(source, destination.as_ref());

    // update the mod file
    fs::write(
        PathBuf::from(destination.as_ref()).join("mod.rs"),
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
}

/// Build a single schema file and write it to the destination file.
///
/// **WARNING: THIS OVERWRITES THE DESTINATION FILE.**
pub fn build_schema(schema: impl AsRef<Path>, destination: impl AsRef<Path>) {
    let (schema, destination) = (schema.as_ref(), destination.as_ref());
    let compiler_path = compiler_path();
    println!("cargo:rerun-if-changed={}", compiler_path.to_str().unwrap());
    println!("cargo:rerun-if-changed={}", schema.to_str().unwrap());
    println!("cargo:rerun-if-changed={}", destination.to_str().unwrap());
    let output = Command::new(compiler_path)
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
}

fn recurse_schema_dir(dir: impl AsRef<Path>, dest: impl AsRef<Path>) -> LinkedList<String> {
    let mut list = LinkedList::new();
    for dir_entry in fs::read_dir(&dir).unwrap() {
        let dir_entry = dir_entry.unwrap();
        let file_type = dir_entry.file_type().unwrap();
        let file_path = PathBuf::from(dir.as_ref()).join(dir_entry.file_name());
        if file_type.is_dir() {
            if dir_entry.file_name() == "ShouldFail" {
                // do nothing
            } else {
                list.append(&mut recurse_schema_dir(&file_path, dest.as_ref()));
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
