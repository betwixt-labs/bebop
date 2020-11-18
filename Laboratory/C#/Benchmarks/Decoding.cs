using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bebop;
using Bebop.Runtime;
using BenchmarkDotNet.Attributes;
using Gateway.Contracts;

namespace Benchmarks
{
    [MinColumn]
    [MaxColumn]
    [MemoryDiagnoser]
    public class Decoding
    {
        private readonly byte[] _testBuffer;
        private const string TestString = @"Hello 明 World!😊 Lorem ipsum dolor sit amet.";

        public Decoding()
        {
           
            _testBuffer = new ChatMessage
            {
                Content = TestString,
                LobbyId = Guid.Parse("02281d74-a708-4f64-8a5c-699100654587"),
                SendTime = DateTime.Now,
            }.Encode();
        }

        [Benchmark]
        public void DynamicDecode()
        {
            _ = BebopMirror.GetRecordFromOpCode(0x54414843).Decode(_testBuffer);
        }

        [Benchmark]
        public void StrongDecode()
        {
            _ = ChatMessage.Decode(_testBuffer);
        }

        [Benchmark]
        public void GenericDecode()
        {
            _ = ChatMessage.DecodeAs<ChatMessage>(_testBuffer);
        }
    }
}
