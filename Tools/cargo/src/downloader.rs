use json::JsonValue;
use std::fs::{self, File};
use std::path::{Path, PathBuf};

#[cfg(target_os = "windows")]
const OS_NAME: &str = "win";
#[cfg(target_os = "linux")]
const OS_NAME: &str = "linux";
#[cfg(target_os = "macos")]
const OS_NAME: &str = "mac";

/// Version of bebopc to download
pub const BEBOPC_VERSION: &str = env!("CARGO_PKG_VERSION");

/// Download the executeable to the destination path.
///
/// - `dest` directory in which bebopc directory structure will be placed.
///
/// The bebopc directoy is structured as
/// - `dest/<version>/<os>/`
///     - <zipfile>
///     - <bin>
///
/// Returns a path to the executable and updates the compiler path static variable.
pub fn download_bebopc(dest: impl AsRef<Path>) -> PathBuf {
    let exe_path = crate::canonicalize(download_bebopc_internal(dest));
    unsafe {
        crate::COMPILER_PATH = Some(exe_path.clone());
    }
    exe_path
}

fn download_bebopc_internal(dest: impl AsRef<Path>) -> PathBuf {
    let root_path = dest.as_ref().join(BEBOPC_VERSION).join(OS_NAME);
    let exe_path = if cfg!(target_os = "windows") {
        root_path.join("bebopc.exe")
    } else {
        root_path.join("bebopc")
    };
    if exe_path.exists() {
        // executable already downloaded
        return exe_path;
    }
    mkdir_p(&root_path);
    let zip_name = format!("bebopc-{}64.zip", OS_NAME);
    let release_info = get_json(format!(
        "https://api.github.com/repos/rainwayapp/bebop/releases/tags/v{}",
        BEBOPC_VERSION
    ));
    let url = release_info["assets"]
        .members()
        .find(|asset| asset["name"].as_str().unwrap() == zip_name)
        .expect("Could not find expected asset")["browser_download_url"]
        .as_str()
        .unwrap()
        .to_owned();

    let zip_path = root_path.join(&zip_name);
    download(url, &zip_path);
    let tmp_path = &root_path.join("tmp");
    unzip(&zip_path, &tmp_path);
    if cfg!(target_os = "windows") {
        mv(&tmp_path.join("bebopc.exe"), &exe_path);
    } else {
        mv(&tmp_path.join("bebopc"), &exe_path);
    };
    rm_rf(&tmp_path);
    exe_path
}

/// Make a directory and parents as required, equiv to `mkdir -p`
fn mkdir_p(path: impl AsRef<Path>) {
    fs::create_dir_all(path).expect("Failed to create directory");
}

/// recursively delete a directory and its contents. Only works if `path` is a directory.
fn rm_rf(path: impl AsRef<Path>) {
    fs::remove_dir_all(path).expect("failed to recursively delete directory")
}

/// Move a file (Does not work with directories on on OSes!)
fn mv(from: impl AsRef<Path>, to: impl AsRef<Path>) {
    if to.as_ref().is_dir() {
        // gotta support windows which is a special child
        // uvm_move_dir::move_dir(from, to).expect("Failed to move directory")
        unimplemented!()
    } else {
        fs::rename(from, to).expect("Failed to mv");
    }
}

/// Extract a `.zip` archive.
fn unzip(path: impl AsRef<Path>, dest: impl AsRef<Path>) {
    let file = File::open(path).expect("Could not access archive file");
    let mut archive = zip::ZipArchive::new(file).expect("Could not read archive file");
    archive
        .extract(dest)
        .expect("Could not extract archive file");
}

/// basically wget, download from URI to file
fn download(uri: impl reqwest::IntoUrl, path: impl AsRef<Path>) {
    let mut res = reqwest::blocking::get(uri).expect("Failed to fetch file");
    let mut file = File::create(path).expect("Failed to create file");
    res.copy_to(&mut file).expect("Failed to write to file");
}

fn get_json(uri: impl reqwest::IntoUrl) -> JsonValue {
    let uri = uri.into_url().unwrap();
    let res = reqwest::blocking::Client::builder()
        .build()
        .unwrap()
        .get(uri.clone())
        .header("User-Agent", "hyper/0.5")
        .send()
        .expect("Failed to GET");
    if res.status() != 200 {
        panic!(
            "Invalid status code from GET {}: {}",
            uri.as_str(),
            res.status()
        )
    }
    json::parse(&res.text().unwrap()).expect("Failed to parse JSON response")
}
