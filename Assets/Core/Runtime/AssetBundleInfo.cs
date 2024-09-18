using System;

namespace Core.Runtime
{
    public class AssetBundleInfo
    {
        /// <summary>
        /// Internal unity bundle name
        /// </summary>
        public string BundleName { get; set; }
        /// <summary>
        /// Friendly display nane, not related to Unity internals
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