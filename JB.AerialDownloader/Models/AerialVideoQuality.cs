using System;

namespace JB.AerialDownloader.Models
{
    [Flags]
    public enum AerialVideoQuality
    {
        SDR1080 = 1,
        HDR1080 = 2,
        SDR4K = 4,
        HDR4K = 8,

        AllSDR = SDR1080 | SDR4K,
        AllHDR = HDR1080 | HDR4K,
        All = AllSDR | AllHDR
    }
}