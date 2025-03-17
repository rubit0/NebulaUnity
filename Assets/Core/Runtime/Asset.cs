using System;
using System.Collections.Generic;

namespace Nebula.Runtime
{
    public class Asset
    {
        /// <summary>
        /// Id of this asset
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Internal name of this container
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Version indicator of this package
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Notes for this release
        /// </summary>
        public string Notes { get; set; } = "";
        /// <summary>
        /// Meta data (optional)
        /// </summary>
        public Dictionary<string, string> MetaData { get; set; } = new();
        /// <summary>
        /// Timestamp to when this container has last changed
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}