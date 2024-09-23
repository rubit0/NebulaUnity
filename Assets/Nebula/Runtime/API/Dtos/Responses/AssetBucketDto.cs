using System;
using System.Collections.Generic;

namespace Nebula.Runtime.API.Dtos.Responses
{
    public class AssetBucketDto
    {
        /// <summary>
        /// Id of this asset bucket
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Display name of this bucket
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Url to the root AssetBundle
        /// </summary>
        public string DataUrl { get; set; }
        /// <summary>
        /// Url to the bundle specific manifest
        /// </summary>
        public string ManifestUrl { get; set; }
        /// <summary>
        /// Internal unity CRC hash128
        /// </summary>
        public string CRC { get; set; }
        /// <summary>
        /// AssetBundles tied to this bucket
        /// </summary>
        public List<AssetBundleDto> AssetBundles { get; set; } = new();
        /// <summary>
        /// Timestamp to when this bundle was last changed on the db
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}
