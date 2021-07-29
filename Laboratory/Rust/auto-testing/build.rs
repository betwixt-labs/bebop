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

    let lib: String = process_schema_dir(SCHEMA_DIR)
        .into_iter()
        .map(|mut schema_name| {
            schema_name.insert_str(0, "pub mod ");
            schema_name.push(';');
            schema_name.push('\n');
            schema_name
        })
        .collect();
    fs::write("./src/lib.rs", &lib).unwrap();
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
            let file_stem = dir_entry.path().file_stem().unwrap().to_str().unwrap().to_string();
            let new_name = format!("_{}", file_stem);
            Command::new(BEBOP_BIN)
                .arg("--files")
                .arg(path.to_str().unwrap())
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
