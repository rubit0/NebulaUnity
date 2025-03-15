using System;
using System.Collections.Generic;

namespace Nebula.Editor.API.Dtos.Responses
{
    public class AssetContainerDto
    {
        /// <summary>
        /// Id of this asset container
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Display name of this asset container
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Free form metadata
        /// </summary>
        public Dictionary<string, string> Meta { get; set; }
        /// <summary>
        /// Package id on release slot dev
        /// </summary>
        public string ReleaseSlotDev { get; set; }
        /// <summary>
        /// Package id on release slot production
        /// </summary>
        public string ReleaseSlotProduction { get; set; }
        /// <summary>
        /// The latest package version
        /// </summary>
        public int LatestReleaseVersion { get; set; }
        /// <summary>
        /// Ids of the groups that can access this container
        /// </summary>
        public List<string> AccessGroups { get; set; }
        /// <summary>
        /// Ids of releases
        /// </summary>
        public List<string> Releases { get; set; }
        /// <summary>
        /// Timestamp to when this bundle was last changed on the db
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}