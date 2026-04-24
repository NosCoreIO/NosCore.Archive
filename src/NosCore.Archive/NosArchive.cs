//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NosCore.Archive
{
    /// <summary>
    /// Read/write support for the legacy NosTale <c>.NOS</c> archive format used
    /// by <c>NScliData_*.NOS</c> and friends. The newer "NT Data" DEFLATE variant
    /// is not supported.
    ///
    /// Layout: <c>int32 fileCount</c>, then per entry
    /// <c>int32 id; int32 nameLen; byte[nameLen] name; int32 unknown; int32 encLen; byte[encLen] enc</c>.
    /// The inner content is XOR-obfuscated — see <see cref="Decrypt"/> and
    /// <see cref="Encrypt"/>. The encrypter emits only the simple 0x33-XOR mode,
    /// which the decrypter handles along with the packed-nibble mode from the
    /// real client.
    /// </summary>
    public static class NosArchive
    {
        /// <summary>
        /// A single file entry inside a legacy <c>.NOS</c> archive.
        /// </summary>
        /// <param name="Id">Per-entry id stored in the archive header (usually the sequential index).</param>
        /// <param name="Name">Inner file name (ASCII), e.g. <c>conststring.dat</c>.</param>
        /// <param name="Unknown">The 4-byte header field between the name and the payload size whose purpose we don't yet characterise — preserve verbatim on round-trip.</param>
        /// <param name="Content">Decrypted payload bytes.</param>
        public sealed record Entry(int Id, string Name, int Unknown, byte[] Content);

        private static readonly byte[] CryptoArray =
        {
            0x00, 0x20, 0x2D, 0x2E, 0x30, 0x31, 0x32, 0x33, 0x34,
            0x35, 0x36, 0x37, 0x38, 0x39, 0x0A, 0x00,
        };

        /// <summary>
        /// Parse a legacy <c>.NOS</c> archive and return its entries with
        /// decrypted <see cref="Entry.Content"/>.
        /// </summary>
        public static List<Entry> Read(byte[] bytes)
        {
            var result = new List<Entry>();
            var i = 0;
            var fileCount = BitConverter.ToInt32(bytes, i); i += 4;
            for (var f = 0; f < fileCount; f++)
            {
                var id = BitConverter.ToInt32(bytes, i); i += 4;
                var nameLen = BitConverter.ToInt32(bytes, i); i += 4;
                var name = Encoding.ASCII.GetString(bytes, i, nameLen);
                i += nameLen;
                var unknown = BitConverter.ToInt32(bytes, i); i += 4;
                var encLen = BitConverter.ToInt32(bytes, i); i += 4;
                var enc = new byte[encLen];
                Buffer.BlockCopy(bytes, i, enc, 0, encLen);
                i += encLen;
                var content = Decrypt(enc);
                result.Add(new Entry(id, name, unknown, content));
            }
            return result;
        }

        /// <summary>
        /// Serialise a list of entries back to the legacy <c>.NOS</c> byte layout,
        /// re-applying the simple 0x33-XOR encryption. The decrypter in the real
        /// client accepts this output.
        /// </summary>
        public static byte[] Write(IReadOnlyList<Entry> entries)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(entries.Count);
            foreach (var e in entries)
            {
                var nameBytes = Encoding.ASCII.GetBytes(e.Name);
                var enc = Encrypt(e.Content);
                w.Write(e.Id);
                w.Write(nameBytes.Length);
                w.Write(nameBytes);
                w.Write(e.Unknown);
                w.Write(enc.Length);
                w.Write(enc);
            }
            return ms.ToArray();
        }

        private static byte[] Decrypt(byte[] enc)
        {
            var output = new List<byte>(enc.Length * 2);
            var i = 0;
            while (i < enc.Length)
            {
                var b = enc[i++];
                if (b == 0xFF)
                {
                    output.Add(0x0D);
                    continue;
                }
                var len = b & 0x7F;
                if ((b & 0x80) != 0)
                {
                    for (; len > 0; len -= 2)
                    {
                        if (i >= enc.Length) break;
                        var c = enc[i++];
                        output.Add(CryptoArray[(c & 0xF0) >> 4]);
                        if (len <= 1) break;
                        var lo = CryptoArray[c & 0x0F];
                        if (lo == 0) break;
                        output.Add(lo);
                    }
                }
                else
                {
                    for (; len > 0; len--)
                    {
                        if (i >= enc.Length) break;
                        output.Add((byte)(enc[i++] ^ 0x33));
                    }
                }
            }
            return output.ToArray();
        }

        private static byte[] Encrypt(byte[] plain)
        {
            using var ms = new MemoryStream(plain.Length * 2);
            var i = 0;
            while (i < plain.Length)
            {
                if (plain[i] == 0x0D)
                {
                    ms.WriteByte(0xFF);
                    i++;
                    continue;
                }
                var start = i;
                while (i < plain.Length && plain[i] != 0x0D && (i - start) < 0x7F)
                {
                    i++;
                }
                var chunkLen = i - start;
                ms.WriteByte((byte)chunkLen);
                for (var j = start; j < i; j++)
                {
                    ms.WriteByte((byte)(plain[j] ^ 0x33));
                }
            }
            return ms.ToArray();
        }
    }
}
