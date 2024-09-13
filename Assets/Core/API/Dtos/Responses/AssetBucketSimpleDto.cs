using System;
using System.Collections.Generic;

namespace Core.API.Dtos.Responses
{
    public class AssetBucketSimpleDto
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
        /// AssetBundle names tied to this bucket
        /// </summary>
        public List<string> AssetBundleIds { get; set; } = new();
        /// <summary>
        /// Timestamp to when this bundle was last changed on the db
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }
    }
}