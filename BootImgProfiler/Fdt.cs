using System;
using System.Text;

namespace BootImgProfiler
{
    /// <summary>
    /// Minimal read-only flattened-device-tree (FDT/DTB) reader, just enough to
    /// pull the base address out of the /memory node.
    /// </summary>
    public static class Fdt
    {
        private const uint FDT_MAGIC = 0xd00dfeed;
        private const uint FDT_BEGIN_NODE = 0x1;
        private const uint FDT_END_NODE = 0x2;
        private const uint FDT_PROP = 0x3;
        private const uint FDT_NOP = 0x4;
        private const uint FDT_END = 0x9;

        /// <summary>Find the first FDT magic (0xd00dfeed, big-endian) at or after start. -1 if none.</summary>
        public static int FindMagic(byte[] data, int start)
        {
            for (int i = Math.Max(0, start); i + 4 <= data.Length; i++)
            {
                if (data[i] == 0xD0 && data[i + 1] == 0x0D &&
                    data[i + 2] == 0xFE && data[i + 3] == 0xED)
                {
                    // Sanity: totalsize must fit inside the file.
                    uint totalSize = ReadU32BE(data, i + 4);
                    if (totalSize >= 40 && (long)i + totalSize <= data.Length)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Walk the FDT at fdtBase and return the /memory node's base address.
        /// Null if there is no readable /memory reg.
        /// </summary>
        public static ulong? ReadMemoryBase(byte[] data, int fdtBase)
        {
            uint offStruct = ReadU32BE(data, fdtBase + 8);
            uint offStrings = ReadU32BE(data, fdtBase + 12);

            int structStart = fdtBase + (int)offStruct;
            int strStart = fdtBase + (int)offStrings;

            int p = structStart;
            int depth = 0;
            uint rootAddrCells = 2; // Device Tree spec default.
            bool inMemory = false;
            int memoryDepth = -1;

            while (p + 4 <= data.Length)
            {
                uint token = ReadU32BE(data, p);
                p += 4;

                if (token == FDT_BEGIN_NODE)
                {
                    string name = ReadCString(data, p, out int nameLen);
                    p += Align4(nameLen + 1);
                    depth++;

                    if (depth == 2 &&
                        (name == "memory" || name.StartsWith("memory@", StringComparison.Ordinal)))
                    {
                        inMemory = true;
                        memoryDepth = depth;
                    }
                }
                else if (token == FDT_END_NODE)
                {
                    if (inMemory && depth == memoryDepth) inMemory = false;
                    depth--;
                    if (depth < 0) break;
                }
                else if (token == FDT_PROP)
                {
                    uint len = ReadU32BE(data, p); p += 4;
                    uint nameOff = ReadU32BE(data, p); p += 4;
                    int valPos = p;
                    p += Align4((int)len);

                    string pname = ReadCString(data, strStart + (int)nameOff, out _);

                    if (depth == 1 && pname == "#address-cells" && len >= 4)
                    {
                        uint c = ReadU32BE(data, valPos);
                        if (c == 1 || c == 2) rootAddrCells = c;
                    }

                    if (inMemory && pname == "reg" && len >= 4 * rootAddrCells)
                    {
                        ulong baseAddr = 0;
                        for (uint i = 0; i < rootAddrCells; i++)
                            baseAddr = (baseAddr << 32) | ReadU32BE(data, valPos + (int)(i * 4));
                        return baseAddr;
                    }
                }
                else if (token == FDT_NOP)
                {
                    // skip
                }
                else if (token == FDT_END)
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException(
                        "unexpected FDT token 0x" + token.ToString("x") +
                        " at offset 0x" + (p - 4).ToString("x"));
                }
            }

            return null;
        }

        private static int Align4(int n) => (n + 3) & ~3;

        private static uint ReadU32BE(byte[] data, int offset)
        {
            return (uint)((data[offset] << 24)
                        | (data[offset + 1] << 16)
                        | (data[offset + 2] << 8)
                        | data[offset + 3]);
        }

        private static string ReadCString(byte[] data, int offset, out int length)
        {
            int i = offset;
            while (i < data.Length && data[i] != 0) i++;
            length = i - offset;
            return Encoding.ASCII.GetString(data, offset, length);
        }
    }
}
