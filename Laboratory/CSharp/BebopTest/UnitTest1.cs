using System;
using Bebop;
using NUnit.Framework;

namespace BebopTest
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


            var output = new BebopView(input.Data);

            Assert.AreEqual(1000, output.ReadInt16());
            Assert.AreEqual(2000, output.ReadInt32());
            Assert.AreEqual(3000, output.ReadUInt64());
            Assert.AreEqual(testString, output.ReadString());
            Assert.AreEqual(testGuid, output.ReadGuid());

            Assert.Pass();
        }
    }
}