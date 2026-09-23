using System;
using System.IO;
using System.Text;

namespace BootImgProfiler
{
    /// <summary>
    /// Result of analysing one image file.
    /// PhysOffset is nullable: it is never in a boot header and is only
    /// recovered from an embedded device tree (DTB) whose /memory base is non-zero.
    /// KernelPhysLoad is only trusted when it looks like an absolute physical
    /// address; a small value such as 0x8000 is a legacy offset placeholder.
    /// </summary>
    public sealed class BootImageInfo
    {
        public string FileName = "";
        public string ImageKind = "unknown";   // boot / vendor_boot / xbl_config / raw
        public uint HeaderVersion;
        public uint PageSize;

        public bool HasKernelAddr;
        public uint RawKernelAddr;
        public bool KernelIsAbsolute;           // true => usable as kernel_phys_load
        public string KernelSource = "n/a";

        public uint? PhysOffset;                // non-zero DTB /memory base, if any
        public string PhysOffsetSource = "not found";

        public string Notes = "";
    }

    public static class BootImage
    {
        private static readonly byte[] AndroidMagic = Encoding.ASCII.GetBytes("ANDROID!");
        private static readonly byte[] VendorMagic = Encoding.ASCII.GetBytes("VNDRBOOT");

        // A kernel_addr at or above this is treated as an absolute physical load
        // address; anything smaller (e.g. 0x8000) is a legacy offset placeholder.
        private const uint AbsoluteKernelThreshold = 0x40000000;

        public static BootImageInfo Analyze(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            var info = new BootImageInfo { FileName = Path.GetFileName(path) };

            if (data.Length >= 44 && StartsWith(data, 0, VendorMagic))
                AnalyzeVendorBoot(data, info);
            else if (data.Length >= 44 && StartsWith(data, 0, AndroidMagic))
                AnalyzeBoot(data, info);
            else
            {
                // Unknown container (e.g. xbl_config.img). Best-effort DTB scan only.
                info.ImageKind = "raw/xbl_config";
                info.Notes = "No ANDROID!/VNDRBOOT magic; scanned for embedded DTB only " +
                             "(this format is not otherwise parsed).";
            }

            RecoverPhysOffset(data, info);
            return info;
        }

        // boot.img (v0..v2): 12 kernel_addr, 36 page_size, 40 header_version.
        // v3/v4 (GKI): no addresses, no DTB.
        private static void AnalyzeBoot(byte[] data, BootImageInfo info)
        {
            info.ImageKind = "boot";
            info.PageSize = ReadU32LE(data, 36);
            info.HeaderVersion = ReadU32LE(data, 40);

            if (info.HeaderVersion >= 3)
            {
                info.KernelSource = "not in header (v3/v4 GKI boot image)";
            }
            else
            {
                info.HasKernelAddr = true;
                info.RawKernelAddr = ReadU32LE(data, 12);
                info.KernelIsAbsolute = info.RawKernelAddr >= AbsoluteKernelThreshold;
                info.KernelSource = "boot header kernel_addr field";
            }
        }

        // vendor_boot.img (v3/v4): 8 header_version, 12 page_size, 16 kernel_addr.
        private static void AnalyzeVendorBoot(byte[] data, BootImageInfo info)
        {
            info.ImageKind = "vendor_boot";
            info.HeaderVersion = ReadU32LE(data, 8);
            info.PageSize = ReadU32LE(data, 12);
            info.HasKernelAddr = true;
            info.RawKernelAddr = ReadU32LE(data, 16);
            info.KernelIsAbsolute = info.RawKernelAddr >= AbsoluteKernelThreshold;
            info.KernelSource = "vendor_boot header kernel_addr field";
        }

        // Scan every embedded FDT and prefer a non-zero /memory base.
        private static void RecoverPhysOffset(byte[] data, BootImageInfo info)
        {
            int search = 0;
            int dtbCount = 0;
            bool sawPlaceholder = false;

            while (true)
            {
                int fdt = Fdt.FindMagic(data, search);
                if (fdt < 0) break;
                dtbCount++;
                try
                {
                    ulong? memBase = Fdt.ReadMemoryBase(data, fdt);
                    if (memBase.HasValue)
                    {
                        uint low = (uint)(memBase.Value & 0xFFFFFFFFUL);
                        if (low != 0)
                        {
                            info.PhysOffset = low;
                            info.PhysOffsetSource =
                                "DTB /memory base @ file offset 0x" + fdt.ToString("x");
                            return;
                        }
                        sawPlaceholder = true;
                    }
                }
                catch { /* skip malformed DTB, keep scanning */ }
                search = fdt + 4;
            }

            if (dtbCount == 0)
                info.PhysOffsetSource = "no embedded DTB";
            else if (sawPlaceholder)
                info.PhysOffsetSource =
                    dtbCount + " DTB(s) found, all /memory reg = 0 (bootloader-filled placeholder)";
            else
                info.PhysOffsetSource = dtbCount + " DTB(s) found, no readable /memory node";
        }

        private static bool StartsWith(byte[] data, int offset, byte[] needle)
        {
            if (offset + needle.Length > data.Length) return false;
            for (int i = 0; i < needle.Length; i++)
                if (data[offset + i] != needle[i]) return false;
            return true;
        }

        private static uint ReadU32LE(byte[] data, int offset)
        {
            return (uint)(data[offset]
                        | (data[offset + 1] << 8)
                        | (data[offset + 2] << 16)
                        | (data[offset + 3] << 24));
        }
    }
}
