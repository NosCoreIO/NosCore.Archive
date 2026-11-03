//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NosCore.Archive
{
    /// <summary>
    /// Read support for the "NT Data" <c>.NOS</c> archive format, whose entries are usually zlib-compressed.
    ///
    /// Layout: <c>"NT Data"</c> magic padded to 16 bytes, <c>int32 fileCount</c>, one byte, then per entry
    /// <c>int32 id; int32 offset</c>. At each offset: <c>int32 unknown; int32 size (unpacked); int32 compressedSize;
    /// byte isCompressed; byte[compressedSize] data</c>, zlib-compressed unless
    /// <c>isCompressed</c> is 0, in which case the <c>size</c> bytes are stored as they are.
    /// </summary>
    public static class NtDataArchive
    {
        /// <summary>
        /// A single file entry inside an "NT Data" archive.
        /// </summary>
        /// <param name="Id">The entry id; these archives store no file names.</param>
        /// <param name="Content">Decompressed payload bytes.</param>
        public sealed record Entry(int Id, byte[] Content);

        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("NT Data");
        private const int CountOffset = 16;
        private const int TableOffset = CountOffset + 5;

        /// <summary>
        /// Whether the bytes start with the "NT Data" magic.
        /// </summary>
        public static bool IsNtData(ReadOnlySpan<byte> bytes) => bytes.StartsWith(Magic);

        /// <summary>
        /// Parse an "NT Data" archive and return its entries decompressed.
        /// </summary>
        public static List<Entry> Read(byte[] bytes)
        {
            if (!IsNtData(bytes))
            {
                throw new InvalidDataException("Not an NT Data archive.");
            }

            var count = BitConverter.ToInt32(bytes, CountOffset);
            if (count < 0 || count > (bytes.Length - TableOffset) / 8)
            {
                throw new InvalidDataException($"NT Data archive declares {count} entries but has room for fewer.");
            }

            var result = new List<Entry>(count);
            for (var i = 0; i < count; i++)
            {
                var row = TableOffset + i * 8;
                var id = BitConverter.ToInt32(bytes, row);
                var offset = BitConverter.ToInt32(bytes, row + 4);
                var size = BitConverter.ToInt32(bytes, offset + 4);
                var compressedSize = BitConverter.ToInt32(bytes, offset + 8);
                var isCompressed = bytes[offset + 12] != 0;
                var data = offset + 13;

                if (!isCompressed)
                {
                    result.Add(new Entry(id, bytes.AsSpan(data, size).ToArray()));
                    continue;
                }

                using var compressed = new MemoryStream(bytes, data, Math.Min(compressedSize, bytes.Length - data));
                using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
                var content = new byte[size];
                zlib.ReadExactly(content);
                if (zlib.ReadByte() != -1)
                {
                    throw new InvalidDataException($"NT Data entry {id} is larger than its declared {size} bytes.");
                }

                result.Add(new Entry(id, content));
            }

            return result;
        }
    }
}
