using System;
using System.Collections.Generic;

namespace Core.Runtime
{
    public class AssetsIndex
    {
        /// <summary>
        /// The remote bucket id
        /// </summary>
        public string BucketId { get; set; }
        /// <summary>
        /// Url to the root AssetBundle
        /// </summary>
        public string DataUrl { get; set; }
        /// <summary>
        /// Url to the root manifest
        /// </summary>
        public string ManifestUrl { get; set; }
        /// <summary>
        /// Internal unity CRC hash128
        /// </summary>
        public string CRC { get; set; }
        /// <summary>
        /// Timestamp to when this bundle was last changed on the backend
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
        /// <summary>
        /// Local AssetBundles
        /// </summary>
        public List<LocalAssetBundleInfo> Bundles { get; set; } = new();
    }
}