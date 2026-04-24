//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NosCore.Archive.Tests
{
    [TestClass]
    public class NosArchiveTests
    {
        [TestMethod]
        public void Roundtrip_SingleEntry_PreservesAllFields()
        {
            var original = new[]
            {
                new NosArchive.Entry(1, "conststring.dat", 0x1234, Encoding.ASCII.GetBytes("hello world"))
            };

            var roundtrip = NosArchive.Read(NosArchive.Write(original));

            Assert.AreEqual(1, roundtrip.Count);
            Assert.AreEqual(original[0].Id, roundtrip[0].Id);
            Assert.AreEqual(original[0].Name, roundtrip[0].Name);
            Assert.AreEqual(original[0].Unknown, roundtrip[0].Unknown);
            CollectionAssert.AreEqual(original[0].Content, roundtrip[0].Content);
        }

        [TestMethod]
        public void Roundtrip_MultipleEntries_PreservesOrder()
        {
            var original = new[]
            {
                new NosArchive.Entry(1, "a.dat", 0, Encoding.ASCII.GetBytes("alpha")),
                new NosArchive.Entry(2, "b.dat", 42, Encoding.ASCII.GetBytes("bravo bravo")),
                new NosArchive.Entry(3, "c.dat", -1, Encoding.ASCII.GetBytes("charlie\r\ndelta"))
            };

            var roundtrip = NosArchive.Read(NosArchive.Write(original));

            Assert.AreEqual(3, roundtrip.Count);
            for (var i = 0; i < original.Length; i++)
            {
                Assert.AreEqual(original[i].Id, roundtrip[i].Id);
                Assert.AreEqual(original[i].Name, roundtrip[i].Name);
                Assert.AreEqual(original[i].Unknown, roundtrip[i].Unknown);
                CollectionAssert.AreEqual(original[i].Content, roundtrip[i].Content);
            }
        }

        [TestMethod]
        public void Roundtrip_CarriageReturnInContent_SurvivesEncoding()
        {
            var payload = Encoding.ASCII.GetBytes("line1\rline2\rline3");
            var original = new[] { new NosArchive.Entry(7, "x.dat", 0, payload) };

            var roundtrip = NosArchive.Read(NosArchive.Write(original));

            CollectionAssert.AreEqual(payload, roundtrip[0].Content);
        }

        [TestMethod]
        public void Roundtrip_EmptyArchive_ReturnsNoEntries()
        {
            var roundtrip = NosArchive.Read(NosArchive.Write(System.Array.Empty<NosArchive.Entry>()));
            Assert.AreEqual(0, roundtrip.Count);
        }

        [TestMethod]
        public void Roundtrip_LongPayload_ExceedingChunkBoundary()
        {
            var payload = new byte[512];
            for (var i = 0; i < payload.Length; i++) payload[i] = (byte)(i % 250 + 1);
            var original = new[] { new NosArchive.Entry(1, "big.dat", 0, payload) };

            var roundtrip = NosArchive.Read(NosArchive.Write(original));

            CollectionAssert.AreEqual(payload, roundtrip[0].Content);
        }
    }
}
