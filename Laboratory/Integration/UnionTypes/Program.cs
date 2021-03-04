using System;
using System.IO;

namespace UnionTypes
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "encode")
            {
                Union1 u = new Right("Success");
                byte[] buffer = Union1.Encode(u);
                using (var stdout = Console.OpenStandardOutput())
                {
                    stdout.Write(buffer, 0, buffer.Length);
                }
                return 0;
            }
            else if (args.Length == 2 && args[0] == "decode")
            {
                byte[] buffer = File.ReadAllBytes(args[1]);
                Union1 u = Union1.Decode(buffer);
                return (u.Value is Right r && r.R == "Success") ? 0 : 1;
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
