using System;
using System.Collections.Generic;

namespace Nebula.Runtime
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
        /// Free string field to define asset type info
        /// </summary>
        public string AssetType { get; set; }
        /// <summary>
        /// Meta data (optional)
        /// </summary>
        public Dictionary<string, string> MetaData { get; set; } = new();
        /// <summary>
        /// The AssetBundles this depends
        /// </summary>
        public List<string> Dependencies { get; set; } = new();
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