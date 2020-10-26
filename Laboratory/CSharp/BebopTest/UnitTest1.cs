using System;
using System.Collections.Generic;
using Bebop;
using NUnit.Framework;

namespace Test
{
    public class Tests
    {

        [SetUp]
        public void Setup()
        {
            
        }

        /// <summary>
        /// Ensures values are being written and read at the correct alignment. 
        /// </summary>
        [Test]
        public void AlignmentTest()
        {
            var testGuid = Guid.Parse("81c6987b-48b7-495f-ad01-ec20cc5f5be1");
            const string testString = @"Hello 明 World!😊";

            var input = BebopView.Create();
            input.WriteInt16(1000);
            input.WriteInt32(2000);
            input.WriteUInt64(3000);
            input.WriteString(testString);
            input.WriteGuid(testGuid);


            var output = BebopView.From(input.Slice());

            Assert.AreEqual(1000, output.ReadInt16());
            Assert.AreEqual(2000, output.ReadInt32());
            Assert.AreEqual(3000, output.ReadUInt64());
            Assert.AreEqual(testString, output.ReadString());
            Assert.AreEqual(testGuid, output.ReadGuid());

            Assert.Pass();
        }

        [Test]
        public void RoundTripMaps()
        {
            var someMaps = new SomeMaps
            {
                M1 = new Dictionary<bool, bool> { { false, true }, { true, false } },
                M2 = new Dictionary<string, Dictionary<string, string>> { { "a", new Dictionary<string, string>{ { "b", "c" } }  }, { "d", new Dictionary<string, string>{ } } },
                M3 = new Dictionary<int, Dictionary<bool, IS>[]>[] { new Dictionary<int, Dictionary<bool, IS>[]> { { 9, new Dictionary<bool, IS>[] { new Dictionary<bool, IS> { { true, new S { X = 1, Y = 2 } } } } } } },
                M4 = new Dictionary<string, float[]>[] { },
                M5 = new Dictionary<System.Guid, IM> { },
            };
            var bytes = SomeMaps.Encode(someMaps);
            var view = BebopView.From(bytes);
            var someMaps2 = SomeMaps.DecodeFrom(ref view);
            Assert.AreEqual(someMaps.M1, someMaps2.M1);
            Assert.AreEqual(someMaps.M2, someMaps2.M2);
            Assert.AreEqual(someMaps.M3[0][9][0][true].Y, someMaps2.M3[0][9][0][true].Y);
            Assert.AreEqual(someMaps.M4, someMaps2.M4);
            Assert.AreEqual(someMaps.M5, someMaps2.M5);
        }
    }
}