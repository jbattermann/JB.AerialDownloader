using System;

namespace JB.AerialDownloader.Models
{
    [Flags]
    public enum AerialVideoQuality
    {
        SDR1080 = 0,
        HDR1080 = 1,
        SDR4K = 2,
        HDR4K = 3,

        AllSDR = SDR1080 | SDR4K,
        AllHDR = HDR1080 | HDR4K,
        All = AllSDR | AllHDR
    }
}