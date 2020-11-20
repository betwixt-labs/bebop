# Bebop runtime for Dart

This runtime contains the helper definitions that are relied on by the Dart code
which `bebopc` generates for your schemas.

All you need to do is add this package to your dependencies, so that the import
statement in the generated code resolves. Then, you call the generated `encode`
and `decode` methods to talk to Bebop — you don't use this package directly.

Try `pub run test`.
