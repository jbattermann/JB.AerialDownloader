using System;

namespace JB.AerialDownloader.Models
{
    public class AerialVideoDownloadUrl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AerialVideoDownloadUrl"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="quality">The quality.</param>
        /// <exception cref="ArgumentNullException">url</exception>
        public AerialVideoDownloadUrl(Uri url, AerialVideoQuality quality)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            Quality = quality;
        }

        /// <summary>
        /// Gets the quality.
        /// </summary>
        /// <value>
        /// The quality.
        /// </value>
        public AerialVideoQuality Quality { get; }
        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public Uri Url { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{Quality} at '{Url}'";
        }
    }
}