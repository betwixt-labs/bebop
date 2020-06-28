using System;
using System.Threading.Tasks;
using Compiler.Parser;

namespace Compiler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var parser = new SchemaParser("C:\\Users\\Andrew\\PierogiVNext\\Schemas\\sample.pie");
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var schema = await parser.Evaluate();
            schema.Validate();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs);
            Console.WriteLine("Hello World!");
        }
    }
}
