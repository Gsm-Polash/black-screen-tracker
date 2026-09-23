using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BootImgProfiler
{
    /// <summary>
    /// Parses the text of /proc/iomem (as captured with
    /// "adb shell su -c \"cat /proc/iomem\"" and saved to a .txt file) and pulls
    /// out the physical RAM base ("System RAM") and, nested under it, the
    /// kernel's physical location ("Kernel code").
    /// </summary>
    public sealed class IomemResult
    {
        public uint? SystemRamBase;   // -> p0_phys_offset
        public uint? KernelCodeBase;  // -> p0_kernel_phys_load
    }

    public static class IomemParser
    {
        private static readonly Regex LineRe =
            new Regex(@"^(?<indent>\s*)(?<start>[0-9a-fA-F]+)-(?<end>[0-9a-fA-F]+)\s*:\s*(?<desc>.+?)\s*$");

        public static IomemResult Parse(string path)
        {
            var result = new IomemResult();
            int ramIndent = -1;
            bool inRamBlock = false;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;
                Match m = LineRe.Match(rawLine);
                if (!m.Success) continue;

                int indent = m.Groups["indent"].Value.Length;
                string desc = m.Groups["desc"].Value;
                uint start = Convert.ToUInt32(m.Groups["start"].Value, 16);

                if (inRamBlock && indent <= ramIndent)
                    inRamBlock = false; // left the System RAM block

                if (!result.SystemRamBase.HasValue &&
                    desc.IndexOf("System RAM", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.SystemRamBase = start;
                    ramIndent = indent;
                    inRamBlock = true;
                    continue;
                }

                if (inRamBlock && !result.KernelCodeBase.HasValue &&
                    desc.IndexOf("Kernel code", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.KernelCodeBase = start;
                }
            }

            return result;
        }
    }
}
