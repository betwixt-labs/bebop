# Bebop - an efficient schema based binary serialization format for building cross-platform, real-time applications.

We know what you're thinking: [yet another standard.](https://xkcd.com/927/) In a world of JSON, Protocol Buffers, MessagePack, and dozens of other formats why reinvent the wheel? 

At Rainway, we stream video games to client applications. At 60 frames per second, video and audio data needs to be delivered every 16 milliseconds. Gamepad, mouse or keyboard input has to be processed and streamed back to a remote host, all in real-time. For us, every millisecond spent encoding and decoding matters. 

Moreover, Rainway runs on a variety of platforms: iOS, Android, embedded devices, web browsers, and more. In each of these environments, we require high performance, and we have to make it easy for our development team to build across many programming languages with confidence.

Our evaluation of other solutions found poor client-side serialization performance, large run-time overhead, poor browser support, and different trade-offs that drove us to create Bebop.

## Fast, Modern, and Strongly Typed

For anyone who's wondering "is JSON good enough for my distributed/networked app's messaging?" it should be just as easy to use Bebop, but faster and safer.

![](https://i.imgur.com/riuqcBC.png)

That speed and safety comes from the Bebop compiler, which turns schemas describing data structures into tightly optimized "encode" and "decode" methods. The generated code is type-safe in languages that support it, and invoking it is dead simple.

Here is an example of what using the Bebop-generated seralization code looks like in TypeScript:

```ts
const mySong: ISong = {
    title: 'Donna Lee',
    year: 1947,
    performers: [
        { name: 'Charlie Parker', plays: Instrument.Sax },
        { name: 'Miles Davis', plays: Instrument.Trumpet },
    ],
};

const buffer = Song.encode(mySong);
const theSameSong = Song.decode(buffer);
``` 

## A Simple Yet Powerful Schema

Syntactically, Bebop schemas (`.bop`) are similar to `.proto` schemas, but with some extra features.

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

Much of the syntax is built to be obvious and convenient. Notably, there are two ways to aggregate data in a Bebop schema:
* Use a `struct` when all fields will always be present, and there will never be more fields (like `struct Point { int32 x; int32 y; }`.)
* Use a `message` when fields can be missing, and it makes sense for more fields to be added in later versions of your app.

Also, Bebop supplies useful base types like `date` and `guid` out of the box. That way, there's no "intermediary serialization step" of converting a date object to a Unix timestamp and back manually — Bebop is all about saving you effort.

You can read the entire Bebop schema language specification here. (TODO)

## Closing

For the majority of people reading this, Bebop is cool. Still, you won't be jumping to integrate it into your existing applications. You've built around other solutions, and it is great they are working for you.

So whether you're starting to build your next multiplayer app or refactor an RPC / IPC, or you need to increase app stability -- for performance and your overall productivity Bebop is here.

- Live Demo (TODO)
- Getting Started in C# (TODO)
- Getting Started in TypeScript (TODO)
- Contributing (TODO)

_Bebop is currently available for C#, Dart, and TypeScript, with official C++ and PHP implementations coming soon._
