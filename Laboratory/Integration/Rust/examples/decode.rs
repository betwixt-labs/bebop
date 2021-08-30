use bebop::prelude::*;
use integration_testing::{make_library, Library};
use std::env;
use std::fs;

fn main() {
    let args: Vec<_> = env::args().collect();
    println!("{}", &args[1]);
    let buf = fs::read(&args[1]).unwrap();
    assert_eq!(Library::deserialize(&buf).unwrap(), make_library());
}
