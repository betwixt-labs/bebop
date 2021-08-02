use fs_extra::copy_items;
use fs_extra::dir::CopyOptions;

const COPY_OPTIONS: CopyOptions = CopyOptions {
    overwrite: true,
    skip_exist: false,
    buffer_size: 64000,
    copy_inside: true,
    content_only: false,
    depth: 0,
};

fn main() {
    copy_items(&["../bebopc"], "./", &COPY_OPTIONS).unwrap();
}
