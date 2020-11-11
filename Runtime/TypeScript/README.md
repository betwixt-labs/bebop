# bebop-ts

This is the Bebop runtime for TypeScript.

It exports the `BebopView` class, which is essentially a fast, Bebop-flavored layer over the `DataView` API. This is used by the code that `bebopc` generates, but you shouldn't need to use `BebopView` directly: you only call the methods on the classes that `bebopc` generates for your schema messages.
