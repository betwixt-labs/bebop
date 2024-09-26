---
title: Union
---

A `union` defines a tagged union of one or more inline struct or message definitions. Each is preceded by a "discriminator" or "tag" value. This defines a type whose values may assume any one of the aggregate layouts defined inside. It corresponds to something like C++'s std::variant or Rust enum.

```bebop
union U { 
    1 -> message A { }
    2 -> struct B {  } 
}
```

The binary representation of a `U` value is then: a length prefix, followed by either (a) a `01` byte followed by an encoding of an `A` message, or (b) a `02` byte followed by an encoding of a `B` struct.

Just like with messages, new branches may be added to a union later. When an unrecognized discriminator value is encountered, the length prefix is used to skip over the body, and decoding fails in a way your program may catch.

Nested types are not available globally but do reserve the identifier globally. E.g. in the above you cannot define struct `Other { A x; }` because `A` is private to `U` but you also cannot define `struct A { }` because A is reserved globally.

:::note
Unions are not yet implemented in some runtimes such as Dart, but are supported even in languages without native union support such as C#
:::