# Pierogi VNext Message Format

# Why?

Pierogi is a binary wire format used by Rainway. Tradtionally, Pierogi today is used to transmit read-only volatile messages such as input, and by Fureai for passing signaling information. 

But there in lies a design flaw. Because Pieorgi today isn't designed to accommodate things like variable length collections, strings, and other "complex" objects, the format forces applications to embed other formats as "payloads."

With multiple formats in use at Rainway (Boring, Sachiel, Pierogi, Quark, etc.) we have to increase the surface area for message serialization, and the subsequent handling of those messages. It doesn't scale, and it makes code harder to maintain.

Finally, Pierogis are today as code, which means the structure of a message must match the definition in a C# codebase. Reimplementation is prone to human error, and can result in increased development overhead.

# Goals

Create the next iteration of the Pierogi format, solving most of the issues mentioned above, and optimizing where other formats such as Google's [Protocol Buffer](https://developers.google.com/protocol-buffers/) fall short. 

 - **Compiler**: VNext features a compiler that is capable of analyzing and validating Pierogi schemas.
 - **Extendable code generation**: A minimal API that allows for code generators to be written for any language.
 - **Efficient encoding**: Rather than storing types at their full width, VNext uses variable-length encoding to create smaller encoded values.
 - **Nesting**: Data structures can contain other data structures, such as a `message` that contains a `struct`.
 - **Attributes**: Attributes can be applied to fields to indicate metadata such as a field being deprecated.
 - **Comments**: Line comments can be added to a schema to provide context to humans.
 - **Detectable fields**: Protocol Buffers cannot detect the presense of data ahead of time, which leads to runtime errors.
 - **Linearizability**: Reading and writing at runtime should occur in a single operation to guarantee time complexity.

# Format

 ## Scalar Types
 
 Pierogi scalar types are aligned with native types found in most programming languages. All scalar types support being used as an array. A scalar type can only contain a "single" value whereas an aggregates can be constructed from multiple scalars (and possibly references to other aggregate kinds).

 - **bool**: A simple type representing Boolean values of true or false.
 - **byte**: An integral type representing unsigned 8-bit integers with values between 0 and 255.
 - **uint**: An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.
 - **int**: An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.
 - **float**: A floating point type representing values ranging from approximately 5.0 x 10 -324 to 1.7 x 10 308 with a precision of 15-16 digits.
 - **string**: A UTF-8 encoded null-terminated string.
 - **guid**: GUID (or UUID) is an acronym for 'Globally Unique Identifier' (or 'Universally Unique Identifier'). It is a 128-bit integer number used to identify resources.

 ## Aggregate Data Types
Aggregate kinds are data structures that can be constructed from multiple scalar type members, and references to other aggregate kinds.

Wikipedia says, "In type theory, a kind is the type of a type constructor or, less commonly, the type of a higher-order type operator. A kind system is essentially a simply typed lambda calculus 'one level up,' endowed with a primitive type, denoted * and called 'type', which is the kind of any (monomorphic) data type."

In layman's terms, a kind is an arity specifier, and the members defined in an aggregate make up an actual Type.

 - **enum**: n enumeration type (or enum type) is a type defined by a set of named constants. It is restricted to a uint integral numeric type.
    - It is possible to add new members to an enum in use by a `message` while maintaining backwards compatibility.
 - **struct**: A structure type (or struct type) is a type that can encapsulate data. All members are guaranteed to be present. The members of the struct are laid out sequentially, and are stored in the order in which they appear.
    - It is not possible for new members to be added to a struct once it is in use by a `message`.
    - Structures should be used when there is a need for performance or a guarantee data is available.
 - **message**: The message type is a data structure that combines state (members) as a single type-safe unit. All members of a message are optional.
    - It is possible to add new members to a members a `message` while maintaining backwards comparability.
    - Messages should be used to model more complex behavior, or data that is intended to be modified.

# Examples

```
package the.more.you.know;

enum VideoCodec {
  H264 = 0;
  H265 = 1;
}

struct VideoData {
  float timestamp;
  uint width;
  uint height;
  byte[] fragment;
}

message MediaMessage {
  VideoCodec codec = 0; 
  string friendlyName = 1 [deprecated];
  VideoData data = 2;
}
```


 