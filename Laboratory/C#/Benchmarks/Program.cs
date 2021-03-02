using System;
using Bebop.Codegen;


var a = new Musician("Travis Scott", Instrument.Clarinet);
var b = new Musician { Name = "Travis Scott", Plays = Instrument.Clarinet};
Console.WriteLine(a == b);