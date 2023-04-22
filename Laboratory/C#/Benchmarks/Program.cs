using System;
using Bebop.Codegen;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;


var a = new Musician("Travis Scott", Instrument.Clarinet);
var b = new Musician { Name = "Travis Scott", Plays = Instrument.Clarinet};
Console.WriteLine(a == b);

var runConfig = DefaultConfig.Instance;

_ = BenchmarkRunner.Run<ObjectReadWrite>(runConfig);
