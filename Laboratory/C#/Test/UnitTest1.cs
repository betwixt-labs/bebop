using System;
using System.Collections.Generic;
using System.Text;
using Bebop.Codegen;
using Bebop.Runtime;
using NUnit.Framework;

namespace Test
{
    enum TestEnum : uint
    {
        Bad = 0,
        Ok = uint.MaxValue
    }
    public class RuntimeTest
    {

        [SetUp]
        public void Setup()
        {

        }

        /// <summary>
        /// Ensures values are being written and read at the correct alignment.
        /// </summary>
        [Test]
        public void WriteRead()
        {
            var testBytes = new byte[] {0x1, 0x2, 0x3};
            var testFloats = new float[] {float.MinValue, float.MaxValue};
            var testDoubles = new double[] { double.MinValue, double.MaxValue };
            var testGuid = Guid.Parse("81c6987b-48b7-495f-ad01-ec20cc5f5be1");
            const string testString = @"Hello 明 World!😊";
            var testDate = DateTime.UtcNow;

            var input = BebopWriter.Create();
            input.WriteByte(false);
            input.WriteByte(byte.MaxValue);
            input.WriteUInt16(ushort.MaxValue);
            input.WriteInt16(short.MaxValue);
            input.WriteUInt32(uint.MaxValue);
            input.WriteInt32(int.MaxValue);
            input.WriteUInt64(ulong.MaxValue);
            input.WriteInt64(long.MaxValue);
            input.WriteFloat32(float.MaxValue);
            input.WriteFloat64(double.MaxValue);
            input.WriteFloat32S(testFloats);
            input.WriteFloat64S(testDoubles);
            input.WriteString(testString);
            input.WriteGuid(testGuid);
            input.WriteDate(testDate);
            input.WriteBytes(testBytes);
            input.WriteEnum(TestEnum.Ok);


            var output = BebopReader.From(input.ToImmutableArray());

            // test byte
            Assert.AreEqual(0, output.ReadByte());
            Assert.AreEqual(byte.MaxValue, output.ReadByte());
            // test short
            Assert.AreEqual(ushort.MaxValue, output.ReadUInt16());
            Assert.AreEqual(short.MaxValue, output.ReadInt16());
            // test int
            Assert.AreEqual(uint.MaxValue, output.ReadUInt32());
            Assert.AreEqual(int.MaxValue, output.ReadInt32());
            // test long
            Assert.AreEqual(ulong.MaxValue, output.ReadUInt64());
            Assert.AreEqual(long.MaxValue, output.ReadInt64());
            // test float / double
            Assert.AreEqual(float.MaxValue, output.ReadFloat32());
            Assert.AreEqual(double.MaxValue, output.ReadFloat64());
             // test float array
            var floatArrayLength = output.ReadUInt32();
            Assert.AreEqual(testFloats.Length, floatArrayLength);
            var parsedFloats = new float[floatArrayLength];
            for (int i = 0; i < floatArrayLength; i++)
            {
                parsedFloats[i] = output.ReadFloat32();
            }
            CollectionAssert.AreEqual(testFloats, parsedFloats);
            // test double array
            var doubleArrayLength = output.ReadUInt32();
            Assert.AreEqual(testDoubles.Length, doubleArrayLength);
            var parsedDoubles = new double[doubleArrayLength];
            for (int i = 0; i < doubleArrayLength; i++)
            {
                parsedDoubles[i] = output.ReadFloat64();
            }
            CollectionAssert.AreEqual(testDoubles, parsedDoubles);
            // test string
            Assert.AreEqual(testString, output.ReadString());
            // test guid
            Assert.AreEqual(testGuid, output.ReadGuid());
            // test date
            Assert.AreEqual(testDate, output.ReadDate());
            // test byte array
            CollectionAssert.AreEqual(testBytes, output.ReadBytes());
            // test enum
            Assert.AreEqual(TestEnum.Ok, output.ReadEnum<TestEnum>());

            Assert.Pass();
        }

        [Test]
        public void RoundTrip()
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
            var library = new Library {Songs = new Dictionary<Guid, Song> {{testGuid, song}}};
            var decodedLibrary = Library.Decode(library.EncodeImmutably());
            Assert.AreEqual(library, decodedLibrary);
        }

        [Test]
        public void FlagsEnum()
        {
            Assert.AreEqual((int)TestFlags.Read, 1);
            Assert.AreEqual((int)TestFlags.Write, 2);
            Assert.AreEqual(TestFlags.ReadWrite, TestFlags.Read | TestFlags.Write);
            // Check that the [Flags] attribute is doing its job:
            Assert.AreEqual((TestFlags.Read | TestFlags.SomethingElse).ToString(), "Read, SomethingElse");
        }
    }
}
