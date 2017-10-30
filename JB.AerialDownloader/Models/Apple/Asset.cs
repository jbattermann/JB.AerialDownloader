namespace JB.AerialDownloader.Models.Apple
{
    internal class Asset
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; private set; }
        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url { get; private set; }
        /// <summary>
        /// Gets the accessibility label.
        /// </summary>
        /// <value>
        /// The accessibility label.
        /// </value>
        public string AccessibilityLabel { get; private set; }
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; private set; }
        /// <summary>
        /// Gets the time of day.
        /// </summary>
        /// <value>
        /// The time of day.
        /// </value>
        public string TimeOfDay { get; private set; }
    }
}