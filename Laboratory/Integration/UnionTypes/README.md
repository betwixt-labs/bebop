This test checks that:

* `Right(r="Success")` encodes to the same binary in all implementations.
* All implementations decode that binary to `Right(r="Success")`.

Run it with `./run_test.sh` (needs npm and dotnet).
