namespace JB.AerialDownloader.Models.Apple
{
    internal class IdAndAssets
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; private set; }
        /// <summary>
        /// Gets the assets.
        /// </summary>
        /// <value>
        /// The assets.
        /// </value>
        public Asset[] Assets { get; private set; }
    }
}