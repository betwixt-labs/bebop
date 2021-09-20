use bebop::prelude::*;
use integration_testing::*;

fn main() {
    let stdout = std::io::stdout();
    make_library().serialize(&mut stdout.lock()).unwrap();
}
