# Bebop - an efficient schema based binary serialization format for building cross-platform, real-time applications.

We know what you're thinking: [yet another standard.](https://xkcd.com/927/) In a world of JSON, Protocol Buffers, MessagePack, and dozens of other formats why reinvent the wheel? 

At Rainway, we stream video games to client applications. Video and audio data needs to be delivered every 16.6 MS. Input from clients such as gamepad, mouse, keyboard, and touch data has to be processed and streamed back to a remote host, all in real-time. For us, every millisecond spent encoding and decoding matters. 

In addition to requiring excellent performance for apps running on iOS, Android, embedded devices, and web browsers, we also have to make it easy for our development team to build across many languages and platforms with confidence.

Our evaluation of other solutions found poor client-side serialization performance, large run-time overhead, poor browser support, and different trade-offs that drove us to create Bebop.

## Fast, Modern, and Strongly Typed

For anyone who's wondering "is JSON good enough for my distributed/networked app's messaging?" it should be just as easy to use Bebop, but faster and safer.

![](https://i.imgur.com/riuqcBC.png)

That speed and safety comes from the Bebop compiler which turns data structures into specialized statically typed models with tightly optimized encode / decode methods. There is no reflection, dynamic code, or arduous type-mapping

```
const song1: ISong = {
    title: 'Donna Lee',
    year: 1947,
    performers: [
        { name: 'Charlie Parker', plays: Instrument.Sax },
        { name: 'Miles Davis', plays: Instrument.Trumpet },
    ],
};

const bebopEncodedSong1 = Song.encode(song1);
``` 
 
The code produced is also zero-copy which reduces garbage collection pressure and bottlenecking allocations.

## A Simple Yet Powerful Schema

Bebop schemas (`.bop`) from a syntax perspective are similar to `.proto` schemas, but with some extra features.

```
enum Instrument {
    Sax = 0;
    Trumpet = 1;
    Clarinet = 2;
}

readonly struct Musician {
    string name;
    Instrument plays;
}

message Song {
    string title = 1;
    uint16 year = 2;
    Musician[] performers = 3;
}

struct Library {
    map[guid, Song] songs;
}
``` 

Bebop features three aggregate types:
- **enum**: A set of named constants of an underlying unsigned integer type.
	- It is possible to add new members to an enum in use by a `message` or `struct` while maintaining backwards compatibility.
- **struct**: A type that can encapsulate data. All members are guaranteed to be present. The members of the struct are laid out sequentially, and are stored in the order in which they appear.
	- when the `readonly` attribute is present on a `struct` the data is immutable after decoding. 
- **message**: A data structure that combines state (members) as a single type-safe unit. All members of a `message` are optional.
	- It is possible to add new members to a `message` while maintaining backwards comparability.
	- The presence of optional data is detectable.

In addition to a plethora of base types that are aligned with native types found in most programming languages:

- **bool**: A simple type representing Boolean values of true or false.
- **byte**: An integral type representing unsigned 8-bit integers with values between 0 and 255.
- **uint16**: An integral type representing unsigned 16-bit integers with values between 0 and 65535.
- **int16**: An integral type representing signed 16-bit integers with values between -32768 and 32767.
- **uint32**: An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.
- **int32**: An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.
- **uint64**: An integral type representing unsigned 64-bit integers with values between 0 and 2^64-1.
- **int64**: An integral type representing signed 64-bit integers with values between -2^63 and 2^63-1.
- **float32**: A 32-bit (single-precision) IEEE 754 floating point number.
- **float64**: A 64-bit (double-precision) IEEE 754 floating point number.
- **string**: A length prefixed UTF-8 encoded string.
- **date**: A type-safe way of representing a 64-bit unix timestamp.
- **guid**: GUID (or UUID) is an acronym for 'Globally Unique Identifier' (or 'Universally Unique Identifier'). It is a 128-bit integer number used to identify resources.
- **T[]**: All base types can be used as arrays by appending the `[]` suffix.
- **map[T1, T2]**: A map type holds key-value pairs.

You can read the entire Bebop schema language specification here.

## Closing

For the majority of people reading this, Bebop is cool. Still, you won't be jumping to integrate it into your existing applications. You've built around other solutions, and it is great they are working for you.

So whether you're starting to build your next multiplayer app or refactor an RPC / IPC, or you need to increase app stability -- for performance and your overall productivity Bebop is here.

- Live Demo (TODO)
- Getting Started in C# (TODO)
- Getting Started in TypeScript (TODO)
- Contributing (TODO)

_Bebop is currently available in C# and TypeScript with official C++, Dart, and PHP implementations releasing soon._
