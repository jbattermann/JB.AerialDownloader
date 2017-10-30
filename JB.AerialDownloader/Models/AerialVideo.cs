using System;
using System.Collections.Generic;

namespace JB.AerialDownloader.Models
{
    public class AerialVideo
    {
        public AerialVideo(string id, AerialVideoDownloadUrl downloadUrl, string accessibilityLabel = "", AerialVideoTimeOfDay timeOfDay = AerialVideoTimeOfDay.Unspecified)
            : this(id, new List<AerialVideoDownloadUrl> { downloadUrl }, accessibilityLabel, timeOfDay)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (downloadUrl == null) throw new ArgumentNullException(nameof(downloadUrl));
        }

        public AerialVideo(string id, ICollection<AerialVideoDownloadUrl> downloadUrls, string accessibilityLabel = "", AerialVideoTimeOfDay timeOfDay = AerialVideoTimeOfDay.Unspecified)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            VideoDownloadUrls = downloadUrls ?? throw new ArgumentNullException(nameof(downloadUrls));
            AccessibilityLabel = accessibilityLabel ?? throw new ArgumentNullException(nameof(accessibilityLabel));
            TimeOfDay = timeOfDay;
        }

        public string Id { get; }
        public ICollection<AerialVideoDownloadUrl> VideoDownloadUrls { get; }
        public AerialVideoTimeOfDay TimeOfDay { get; }
        public string AccessibilityLabel { get; }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{(!string.IsNullOrWhiteSpace(AccessibilityLabel) ? AccessibilityLabel : "<No Label>")} by {(TimeOfDay != AerialVideoTimeOfDay.Unspecified ? TimeOfDay.ToString() : "<No Time of Day>")} ({(!string.IsNullOrWhiteSpace(Id) ? Id : "<No Id>")}) with {VideoDownloadUrls.Count} Url(s)";
        }
    }
}