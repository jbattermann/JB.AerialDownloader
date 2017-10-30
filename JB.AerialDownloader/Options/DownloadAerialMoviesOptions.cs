using CommandLine;
using JB.AerialDownloader.Models;

namespace JB.AerialDownloader.Options
{
    [Verb("download", HelpText = "Downloads Aerial Movies for the given JSON Url.")]
    public class DownloadAerialMoviesOptions : IOptions
    {
        [Option(Required = true, HelpText = "URL to the Json file to download, parse and extract Aerial movies URLs from.")]
        public string JsonUrl { get; set; }

        [Option(Required = true, HelpText = "Path to (existing) output directory.")]
        public string Output { get; set; }

        [Option(Required = false, Default = false, HelpText = "If enabled, existing file(s) will be overwritten.")]
        public bool Force { get; set; }

        [Option(Required = false, Default = 1, HelpText = "Specifies the maximum level of concurrency for downloads and writes.")]
        public int MaxDegreeOfParallelism { get; set; }

        [Option(Required = false, Default = AerialVideoQuality.SDR1080, HelpText = "Specifies the quality and type of video(s) to download (Qualities supported are: SDR1080, HDR1080, SDR4K, HDR4K, AllSDR, AllHDR, All).")]
        public AerialVideoQuality Quality { get; set; }
    }
}