using System;
using System.Collections.Generic;

namespace Nebula.Runtime.API.Dtos.Responses
{
    public class ReleaseDto
    {
        /// <summary>
        /// Id of this asset bucket
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Incremental version indicator of this package
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Notes for this release
        /// </summary>
        public string Notes { get; set; }
        /// <summary>
        /// Packages in this release
        /// </summary>
        public List<PackageDto> Packages { get; set; }
        /// <summary>
        /// Timestamp to when this bundle was last changed on the db
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}