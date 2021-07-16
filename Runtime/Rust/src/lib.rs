mod serialization;
mod types;
pub use types::*;
pub use serialization::*;

#[cfg(test)]
mod tests {
    #[test]
    fn it_works() {
        assert_eq!(2 + 2, 4);
    }
}
