using System;
using System.Threading.Tasks;
using Compiler.Lexer;
using Compiler.Lexer.Tokenization;
using Compiler.Parser;

namespace Compiler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var parser = new SchemaParser("C:\\Users\\Andrew\\source\\repos\\PierogivNext\\Schemas\\sample.pie");
            var watch = System.Diagnostics.Stopwatch.StartNew();

            await parser.Evaluate();
          
           
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs);
            Console.WriteLine("Hello World!");
        }
    }
}
