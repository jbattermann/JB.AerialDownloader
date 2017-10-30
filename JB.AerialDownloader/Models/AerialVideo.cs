using System;
using System.Collections.Generic;

namespace JB.AerialDownloader.Models
{
    public class AerialVideo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AerialVideo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="downloadUrl">The download URL.</param>
        /// <param name="accessibilityLabel">The accessibility label.</param>
        /// <param name="timeOfDay">The time of day.</param>
        /// <exception cref="ArgumentNullException">
        /// id
        /// or
        /// downloadUrl
        /// </exception>
        public AerialVideo(string id, AerialVideoDownloadUrl downloadUrl, string accessibilityLabel = "", AerialVideoTimeOfDay timeOfDay = AerialVideoTimeOfDay.Unspecified)
            : this(id, new List<AerialVideoDownloadUrl> { downloadUrl }, accessibilityLabel, timeOfDay)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (downloadUrl == null) throw new ArgumentNullException(nameof(downloadUrl));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AerialVideo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="downloadUrls">The download urls.</param>
        /// <param name="accessibilityLabel">The accessibility label.</param>
        /// <param name="timeOfDay">The time of day.</param>
        /// <exception cref="ArgumentNullException">
        /// id
        /// or
        /// downloadUrls
        /// or
        /// accessibilityLabel
        /// </exception>
        public AerialVideo(string id, ICollection<AerialVideoDownloadUrl> downloadUrls, string accessibilityLabel = "", AerialVideoTimeOfDay timeOfDay = AerialVideoTimeOfDay.Unspecified)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            VideoDownloadUrls = downloadUrls ?? throw new ArgumentNullException(nameof(downloadUrls));
            AccessibilityLabel = accessibilityLabel ?? throw new ArgumentNullException(nameof(accessibilityLabel));
            TimeOfDay = timeOfDay;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; }
        /// <summary>
        /// Gets the video download urls.
        /// </summary>
        /// <value>
        /// The video download urls.
        /// </value>
        public ICollection<AerialVideoDownloadUrl> VideoDownloadUrls { get; }
        /// <summary>
        /// Gets the time of day.
        /// </summary>
        /// <value>
        /// The time of day.
        /// </value>
        public AerialVideoTimeOfDay TimeOfDay { get; }
        /// <summary>
        /// Gets the accessibility label.
        /// </summary>
        /// <value>
        /// The accessibility label.
        /// </value>
        public string AccessibilityLabel { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{(!string.IsNullOrWhiteSpace(AccessibilityLabel) ? AccessibilityLabel : "<No Label>")} by {(TimeOfDay != AerialVideoTimeOfDay.Unspecified ? TimeOfDay.ToString() : "<No Time of Day>")} ({(!string.IsNullOrWhiteSpace(Id) ? Id : "<No Id>")}) with {VideoDownloadUrls.Count} Url(s)";
        }
    }
}