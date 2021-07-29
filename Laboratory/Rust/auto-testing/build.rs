use std::collections::LinkedList;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;

const SCHEMA_DIR: &str = "../../Schemas";

#[cfg(windows)]
const BEBOP_BIN: &str = "../../../bin/compiler/Windows-Debug/bebopc.exe";
#[cfg(unix)]
const BEBOP_BIN: &str = "../../../../bin/compiler/Linux-Debug/bebopc";

fn main() {
    println!("cargo:rerun-if-changed={}", BEBOP_BIN);
    // clean all previously built files
    fs::read_dir("src")
        .unwrap()
        .filter_map(|entry| {
            let entry = entry.unwrap();
            let name = entry.file_name().to_str().unwrap().to_string();
            if entry.file_type().unwrap().is_file()
                && name.starts_with("_")
                && name.ends_with(".rs")
            {
                Some(name)
            } else {
                None
            }
        })
        .for_each(|file| fs::remove_file(format!("src/{}", file)).unwrap());

    // build all files and update lib.rs
    let files = process_schema_dir(SCHEMA_DIR);

    // update the lib file
    fs::write(
        "./src/lib.rs",
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

fn process_schema_dir(dir: impl AsRef<Path>) -> LinkedList<String> {
    let mut list = LinkedList::new();
    for dir_entry in fs::read_dir(dir.as_ref()).unwrap() {
        let dir_entry = dir_entry.unwrap();
        let file_type = dir_entry.file_type().unwrap();
        let path = PathBuf::from(dir.as_ref()).join(dir_entry.file_name());
        if file_type.is_dir() {
            if dir_entry.file_name() == "ShouldFail" {
                // do nothing
            } else {
                list.append(&mut process_schema_dir(path));
            }
        } else if file_type.is_file() {
            let file_stem = dir_entry
                .path()
                .file_stem()
                .unwrap()
                .to_str()
                .unwrap()
                .to_string();
            let new_name = format!("_{}", file_stem);
            let schema_path = path.to_str().unwrap();
            println!("cargo:rerun-if-changed={}", schema_path);
            Command::new(BEBOP_BIN)
                .arg("--files")
                .arg(schema_path)
                .arg("--rust")
                .arg(format!("./src/{}.rs", &new_name))
                .output()
                .expect("failed to build schema");
            list.push_back(new_name);
        } else {
            // do nothing
        }
    }
    list
}