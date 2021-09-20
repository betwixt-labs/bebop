using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bebop;
using Bebop.Codegen;
using Bebop.Runtime;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks
{
    [MinColumn]
    [MaxColumn]
    [MemoryDiagnoser]
    //[SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class ObjectReadWrite
    {
        private readonly Library _library;
        private readonly byte[] _bufferToGrow;
        private readonly byte[] _bufferFittingMessage;

        public ObjectReadWrite()
        {
            var testGuid = Guid.Parse("81c6987b-48b7-495f-ad01-ec20cc5f5be1");
            var song = new Song
            {
                Title = "Donna Lee",
                Year = 1974,
                Performers = new Musician[]
                {
                    new Musician {Name = "Charlie Parker", Plays = Instrument.Sax},
                    new Musician {Name = "Miles Davis", Plays = Instrument.Trumpet}
                }
            };

            _library = new Library { Songs = new Dictionary<Guid, Song> { { testGuid, song } } };

            _bufferToGrow = new byte[1];
            _bufferFittingMessage = new byte[1_000];
        }

        [Benchmark]
        public byte[] EncodeOriginal()
            => _library.Encode();

        [Benchmark]
        public byte[] EncodeWithGivenLength_NotGrownDuringWrite()
            => _library.Encode(1_000);

        //[Benchmark]
        //public byte[] EncodeWithGivenLength_GrownDuringWrite()
        //    => _library.Encode(100);

        /// <summary>
        /// Benchmark passing in a buffer that will fit the serialized record and will not be grown
        /// </summary>
        [Benchmark]
        public int EncodeWithGivenArray_NotGrownDuringWrite()
            => _library.EncodeIntoBuffer(_bufferFittingMessage);

        ///// <summary>
        ///// Benchmark passing in a buffer that will be grown by the Bebop writer (aka makes a new instance of the buffer)
        ///// </summary>
        //[Benchmark]
        //public int EncodeWithGivenArray_GrownDuringWrite()
        //    => _library.EncodeIntoBuffer(_bufferToGrow);
    }
}
