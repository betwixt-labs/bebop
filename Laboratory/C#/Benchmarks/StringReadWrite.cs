using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bebop;
using Bebop.Runtime;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MinColumn]
    [MaxColumn]
    [MemoryDiagnoser]
    public class StringReadWrite
    {
        private readonly byte[] _testBuffer;
        private const string TestString = @"Hello 明 World!😊 Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam sed maximus purus. Nullam in sodales dolor. Pellentesque a ex dapibus, auctor purus eu, laoreet tortor. Nullam urna sapien, aliquet id consectetur at, sollicitudin et lorem. Sed in congue leo, sit amet vehicula nisi. Sed in nunc convallis, porttitor mi et, sollicitudin ipsum. Vivamus diam risus, tempor pharetra malesuada vitae, placerat rutrum nulla. Morbi id purus nec purus auctor molestie. Mauris rhoncus leo in elit pulvinar finibus porttitor sed massa. Nam tempus commodo molestie. Pellentesque posuere justo tempor augue tempus ultricies. Sed aliquet lectus sed ipsum scelerisque interdum. Morbi rutrum in ligula id efficitur.";

        public StringReadWrite()
        {
            var input = BebopWriter.Create();
            input.WriteString(TestString);
            _testBuffer = input.ToArray();
        }

        [Benchmark]
        public void Write()
        {
            var input = BebopWriter.Create();
            input.WriteString(TestString);
        }

        [Benchmark]
        public void Read()
        {
            var output = BebopReader.From(_testBuffer);
            _ = output.ReadString();
        }
    }
}
