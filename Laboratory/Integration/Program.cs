using System;
using System.Collections.Generic;
using System.IO;
using Bebop.Runtime;

namespace IntegrationTesting
{
    class Program
    {
        static Library MakeLibrary() =>
            new Library(
                new Dictionary<string, Album>
                {
                    ["Giant Steps"] = new StudioAlbum(
                        new[]
                        {
                            new Song("Giant Steps", 1959,
                                new[] { new Musician("John Coltrane", Instrument.Piano) }),
                            new Song("A Night in Tunisia", 1942,
                                new[]
                                {
                                    new Musician("Dizzy Gillespie", Instrument.Trumpet),
                                    new Musician("Count Basie", Instrument.Piano)
                                }),
                            new Song("Groovin' High", null, null)
                        }
                    ),
                    ["Adam's Apple"] = new LiveAlbum(null, "Tunisia", DateTime.FromFileTimeUtc(121726790790000000)),
                    ["Milestones"] = new StudioAlbum(Array.Empty<Song>()),
                    ["Brilliant Corners"] = new LiveAlbum(
                        new[]
                        {
                            new Song(null, 1965,
                                new[]
                                {
                                    new Musician("Carmell Jones", Instrument.Trumpet),
                                    new Musician("Joe Henderson", Instrument.Sax),
                                    new Musician("Teddy Smith", Instrument.Clarinet)
                                })
                        }, "Night's Palace", null)
                });

        static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "encode")
            {
                var lib = MakeLibrary();
                byte[] buffer = Library.Encode(lib);
                using (var stdout = Console.OpenStandardOutput())
                {
                    stdout.Write(buffer, 0, buffer.Length);
                }

                return 0;
            }
            else if (args.Length == 2 && args[0] == "decode")
            {
                byte[] buffer = File.ReadAllBytes(args[1]);
                var lib = Library.Decode(buffer);
                return lib.Equals(MakeLibrary()) ? 0 : 1;
            }
            else
            {
                var name = Environment.GetCommandLineArgs()[0];
                Console.WriteLine($"Usage:\n  {name} encode\n  {name} decode file.buf");
                return 1;
            }
        }
    }
}