using System;

namespace Core.API.Dtos.Responses
{
    public class AssetBundleDto
    {
        /// <summary>
        /// Id and bundle name
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Friendly display bane, not related to Unity internals
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Auto incremental version
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Internal unity CRC hash128
        /// </summary>
        public string CRC { get; set; }
        /// <summary>
        /// Url to the bundle
        /// </summary>
        public string DataUrl { get; set; }
        /// <summary>
        /// Url to the bundle specific manifest
        /// </summary>
        public string ManifestUrl { get; set; }
        /// <summary>
        /// Timestamp to when this bundle was last changed on the db
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}