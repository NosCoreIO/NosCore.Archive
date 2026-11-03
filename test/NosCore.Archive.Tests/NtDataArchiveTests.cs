//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NosCore.Archive.Tests
{
    [TestClass]
    public class NtDataArchiveTests
    {
        private static byte[] Build(params (int Id, byte[] Content)[] files) => Build(true, files);

        private static byte[] Build(bool compress, params (int Id, byte[] Content)[] files)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(Encoding.ASCII.GetBytes("NT Data"));
            w.Write(new byte[9]);
            w.Write(files.Length);
            w.Write((byte)0);
            var tableStart = ms.Position;
            w.Write(new byte[files.Length * 8]);

            var offsets = new List<int>();
            foreach (var (_, content) in files)
            {
                using var compressed = new MemoryStream();
                using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(content);
                }

                var stored = compress ? compressed.ToArray() : content;
                offsets.Add((int)ms.Position);
                w.Write(0);
                w.Write(content.Length);
                w.Write(stored.Length);
                w.Write((byte)(compress ? 1 : 0));
                w.Write(stored);
            }

            ms.Position = tableStart;
            for (var i = 0; i < files.Length; i++)
            {
                w.Write(files[i].Id);
                w.Write(offsets[i]);
            }

            return ms.ToArray();
        }

        [TestMethod]
        public void EntriesAreDecompressedInTableOrder()
        {
            var first = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("map data ", 200)));
            var second = new byte[] { 0, 1, 2, 3, 255 };

            var entries = NtDataArchive.Read(Build((7, first), (3, second)));

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual(7, entries[0].Id);
            CollectionAssert.AreEqual(first, entries[0].Content);
            Assert.AreEqual(3, entries[1].Id);
            CollectionAssert.AreEqual(second, entries[1].Content);
        }

        [TestMethod]
        public void UncompressedEntriesAreReadAsStored()
        {
            var content = Encoding.ASCII.GetBytes("stored as is");

            var entries = NtDataArchive.Read(Build(false, (0, content), (1, content)));

            Assert.AreEqual(2, entries.Count);
            CollectionAssert.AreEqual(content, entries[0].Content);
            CollectionAssert.AreEqual(content, entries[1].Content);
        }

        [TestMethod]
        public void AnEmptyArchiveHasNoEntries()
        {
            Assert.AreEqual(0, NtDataArchive.Read(Build()).Count);
        }

        [TestMethod]
        public void TheMagicIsRecognised()
        {
            Assert.IsTrue(NtDataArchive.IsNtData(Build()));
            Assert.IsFalse(NtDataArchive.IsNtData(NosArchive.Write([])));
        }

        [TestMethod]
        public void ACountLargerThanTheTableIsRejected()
        {
            var bytes = Build();
            System.BitConverter.GetBytes(int.MaxValue).CopyTo(bytes, 16);

            Assert.ThrowsExactly<InvalidDataException>(() => NtDataArchive.Read(bytes));
        }

        [TestMethod]
        public void AnEntryLargerThanItsDeclaredSizeIsRejected()
        {
            var bytes = Build((1, new byte[100]));
            var offset = System.BitConverter.ToInt32(bytes, 25);
            System.BitConverter.GetBytes(10).CopyTo(bytes, offset + 4);

            Assert.ThrowsExactly<InvalidDataException>(() => NtDataArchive.Read(bytes));
        }

        [TestMethod]
        public void ALegacyArchiveIsRejected()
        {
            Assert.ThrowsExactly<InvalidDataException>(() => NtDataArchive.Read(NosArchive.Write([])));
        }
    }
}
