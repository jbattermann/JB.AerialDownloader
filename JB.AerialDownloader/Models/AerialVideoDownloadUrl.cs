using System;

namespace JB.AerialDownloader.Models
{
    public class AerialVideoDownloadUrl
    {
        public AerialVideoDownloadUrl(Uri url, AerialVideoQuality quality)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            Quality = quality;
        }

        public AerialVideoQuality Quality { get; }
        public Uri Url { get; }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Quality} at '{Url}'";
        }
    }
}