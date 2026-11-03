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
    /// Read support for the "NT Data" <c>.NOS</c> archive format, whose entries are zlib-compressed.
    ///
    /// Layout: <c>"NT Data"</c> magic padded to 16 bytes, <c>int32 fileCount</c>, one byte, then per entry
    /// <c>int32 id; int32 offset</c>. At each offset: <c>int32 unknown; int32 size; int32 compressedSize;
    /// byte flag; byte[compressedSize] zlib data</c>.
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
            var result = new List<Entry>(count);
            for (var i = 0; i < count; i++)
            {
                var row = TableOffset + i * 8;
                var id = BitConverter.ToInt32(bytes, row);
                var offset = BitConverter.ToInt32(bytes, row + 4);
                var compressedSize = BitConverter.ToInt32(bytes, offset + 8);
                var data = offset + 13;

                using var compressed = new MemoryStream(bytes, data, Math.Min(compressedSize, bytes.Length - data));
                using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
                using var content = new MemoryStream();
                zlib.CopyTo(content);
                result.Add(new Entry(id, content.ToArray()));
            }

            return result;
        }
    }
}
