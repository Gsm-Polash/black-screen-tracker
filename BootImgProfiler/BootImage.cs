using System;
using System.IO;
using System.Text;

namespace BootImgProfiler
{
    /// <summary>
    /// Result of analysing an Android boot / vendor_boot image.
    /// PhysOffset is nullable because it is NOT stored in any boot header and
    /// can only be recovered when the image carries an embedded device tree (DTB).
    /// </summary>
    public sealed class BootImageInfo
    {
        public string ImageKind = "unknown"; // "boot" or "vendor_boot"
        public uint HeaderVersion;
        public uint PageSize;

        // Physical load address of the kernel (kernel_addr field).
        public uint KernelPhysLoad;
        public string KernelSource = "";

        // Recovered from an embedded DTB /memory node, if present. Null otherwise.
        public uint? PhysOffset;
        public string PhysOffsetSource = "not found";

        public string Notes = "";
    }

    public static class BootImage
    {
        private static readonly byte[] AndroidMagic =
            Encoding.ASCII.GetBytes("ANDROID!"); // boot.img, offset 0
        private static readonly byte[] VendorMagic =
            Encoding.ASCII.GetBytes("VNDRBOOT"); // vendor_boot.img, offset 0

        public static BootImageInfo Analyze(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            if (data.Length < 44)
                throw new InvalidDataException("File is too small to be a boot image.");

            if (StartsWith(data, 0, VendorMagic))
                return AnalyzeVendorBoot(data);
            if (StartsWith(data, 0, AndroidMagic))
                return AnalyzeBoot(data);

            throw new InvalidDataException(
                "Neither \"ANDROID!\" nor \"VNDRBOOT\" magic found at offset 0. " +
                "This is not a boot.img or vendor_boot.img.");
        }

        // ---- boot.img (magic "ANDROID!") -------------------------------------
        // Classic boot_img_hdr (v0..v2), little-endian:
        //   0 magic[8] | 8 kernel_size | 12 kernel_addr | 16 ramdisk_size
        //   20 ramdisk_addr | 24 second_size | 28 second_addr | 32 tags_addr
        //   36 page_size | 40 header_version
        // v3/v4 dropped the per-section addresses (GKI): no kernel_addr, no dtb.
        private static BootImageInfo AnalyzeBoot(byte[] data)
        {
            var info = new BootImageInfo { ImageKind = "boot" };
            info.PageSize = ReadU32LE(data, 36);
            info.HeaderVersion = ReadU32LE(data, 40);

            if (info.HeaderVersion >= 3)
            {
                info.KernelSource = "not in header (v3/v4 GKI boot image)";
                info.Notes =
                    "This is a header v" + info.HeaderVersion + " boot image, which stores " +
                    "no load addresses and no DTB. Use this device's vendor_boot.img " +
                    "instead: its header holds kernel_addr and it carries the DTB.";
            }
            else
            {
                info.KernelPhysLoad = ReadU32LE(data, 12);
                info.KernelSource = "boot header kernel_addr field";
            }

            RecoverPhysOffset(data, info);
            return info;
        }

        // ---- vendor_boot.img (magic "VNDRBOOT") ------------------------------
        // vendor_boot_img_hdr v3/v4, little-endian:
        //   0 magic[8] | 8 header_version | 12 page_size | 16 kernel_addr
        //   20 ramdisk_addr | 24 vendor_ramdisk_size | 28 cmdline[2048]
        //   2076 tags_addr | 2080 name[16] | 2096 header_size
        //   2100 dtb_size | 2104 dtb_addr(u64)
        // The DTB is a separate page-aligned section; we locate it by scanning
        // for the FDT magic, which is robust across v3/v4 layout differences.
        private static BootImageInfo AnalyzeVendorBoot(byte[] data)
        {
            var info = new BootImageInfo { ImageKind = "vendor_boot" };
            info.HeaderVersion = ReadU32LE(data, 8);
            info.PageSize = ReadU32LE(data, 12);
            info.KernelPhysLoad = ReadU32LE(data, 16);
            info.KernelSource = "vendor_boot header kernel_addr field";

            RecoverPhysOffset(data, info);
            return info;
        }

        // phys_offset is never in a boot header. Recover it from an embedded FDT.
        private static void RecoverPhysOffset(byte[] data, BootImageInfo info)
        {
            int fdtOffset = Fdt.FindMagic(data, 0);
            if (fdtOffset < 0)
            {
                info.PhysOffsetSource =
                    "no embedded DTB in this image; phys_offset must come from the " +
                    "device tree / SoC memory map (try vendor_boot.img or dtb.img)";
                return;
            }

            try
            {
                ulong? memBase = Fdt.ReadMemoryBase(data, fdtOffset);
                if (memBase.HasValue)
                {
                    info.PhysOffset = (uint)(memBase.Value & 0xFFFFFFFFUL);
                    info.PhysOffsetSource =
                        "embedded DTB /memory node @ file offset 0x" + fdtOffset.ToString("x");
                }
                else
                {
                    info.PhysOffsetSource = "embedded DTB found but no readable /memory node";
                }
            }
            catch (Exception ex)
            {
                info.PhysOffsetSource = "DTB parse failed: " + ex.Message;
            }
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
