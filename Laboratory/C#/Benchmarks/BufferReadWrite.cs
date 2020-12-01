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
    public class BufferReadWrite
    {
        private readonly byte[] _testBuffer;
        private readonly Guid _testGuid;
        private const string TestString = @"Hello 明 World!😊 Lorem ipsum dolor sit amet.";

        public BufferReadWrite()
        {
            _testGuid = Guid.Parse("02281d74-a708-4f64-8a5c-699100654587");
            var input = BebopWriter.Create();
            input.WriteByte(0);
            input.WriteInt16(1000);
            input.WriteInt32(2000);
            input.WriteString(TestString);
            input.WriteGuid(_testGuid);
            _testBuffer = input.ToArray();
        }

        [Benchmark]
        public void WriteBuffer()
        {
            var input = BebopWriter.Create();
            input.WriteByte(0);
            input.WriteInt16(1000);
            input.WriteInt32(2000);
            input.WriteString(TestString);
            input.WriteGuid(_testGuid);
        }

        [Benchmark]
        public void ReadBuffer()
        {
            var output = BebopReader.From(_testBuffer);
            _ = output.ReadByte();
            _ = output.ReadInt16();
            _ = output.ReadInt32();
            _ = output.ReadString();
            _ = output.ReadGuid();
        }
    }
}
