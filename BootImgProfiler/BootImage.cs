using System;
using System.IO;
using System.Text;

namespace BootImgProfiler
{
    /// <summary>
    /// Result of analysing an Android boot image.
    /// PhysOffset is nullable because it is NOT stored in the boot header and
    /// can only be recovered when the image carries an embedded device tree (DTB).
    /// </summary>
    public sealed class BootImageInfo
    {
        public uint HeaderVersion;
        public uint PageSize;

        // kernel_addr field from the boot header = physical load address of the kernel.
        public uint KernelPhysLoad;

        // Recovered from an embedded DTB /memory node, if present. Null otherwise.
        public uint? PhysOffset;

        public string PhysOffsetSource = "not found";
        public string Notes = "";
    }

    public static class BootImage
    {
        private static readonly byte[] AndroidMagic =
            Encoding.ASCII.GetBytes("ANDROID!"); // 8 bytes, offset 0

        /// <summary>
        /// Parse a boot.img. Throws on a file that is not a recognisable boot image.
        /// </summary>
        public static BootImageInfo Analyze(string path)
        {
            byte[] data = File.ReadAllBytes(path);

            if (data.Length < 44)
                throw new InvalidDataException("File is too small to be a boot image.");

            if (!StartsWith(data, 0, AndroidMagic))
            {
                throw new InvalidDataException(
                    "\"ANDROID!\" magic not found at offset 0. " +
                    "This is not a standard Android boot image (it may be a " +
                    "vendor_boot, a raw kernel, or a different format).");
            }

            var info = new BootImageInfo();

            // Classic boot_img_hdr (v0..v2) layout, all little-endian:
            //   0  : magic[8]
            //   8  : kernel_size      (u32)
            //   12 : kernel_addr      (u32)  <-- physical load address of the kernel
            //   16 : ramdisk_size     (u32)
            //   20 : ramdisk_addr     (u32)
            //   24 : second_size      (u32)
            //   28 : second_addr      (u32)
            //   32 : tags_addr        (u32)
            //   36 : page_size        (u32)
            //   40 : header_version   (u32)
            info.KernelPhysLoad = ReadU32LE(data, 12);
            info.PageSize = ReadU32LE(data, 36);
            info.HeaderVersion = ReadU32LE(data, 40);

            // header v3/v4 changed the layout (no per-section addresses), so
            // offset 12 is not kernel_addr there. Flag that clearly instead of
            // reporting a wrong value.
            if (info.HeaderVersion >= 3)
            {
                info.KernelPhysLoad = 0;
                info.Notes =
                    "Header version " + info.HeaderVersion +
                    " does not store load addresses in the boot header, so " +
                    "kernel_phys_load cannot be read from it.";
            }

            // phys_offset is never in the boot header. Try to recover it from an
            // embedded flattened device tree (FDT) /memory node.
            int fdtOffset = Fdt.FindMagic(data, 0);
            if (fdtOffset >= 0)
            {
                try
                {
                    ulong? memBase = Fdt.ReadMemoryBase(data, fdtOffset);
                    if (memBase.HasValue)
                    {
                        info.PhysOffset = (uint)(memBase.Value & 0xFFFFFFFFUL);
                        info.PhysOffsetSource =
                            "embedded DTB /memory node @ file offset 0x" +
                            fdtOffset.ToString("x");
                    }
                    else
                    {
                        info.PhysOffsetSource =
                            "embedded DTB found but no readable /memory node";
                    }
                }
                catch (Exception ex)
                {
                    info.PhysOffsetSource = "DTB parse failed: " + ex.Message;
                }
            }
            else
            {
                info.PhysOffsetSource =
                    "no embedded DTB in this image; phys_offset must come from " +
                    "the device tree / SoC memory map (not stored in boot.img)";
            }

            return info;
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
