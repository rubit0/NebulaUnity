using System.Collections.Generic;

namespace Nebula.Runtime.API.Dtos.Requests
{
    public class UploadAssetBundleDto
    {
        public string BundleName { get; set; }
        public string DisplayName { get; set; }
        public string AssetType { get; set; }
        public Dictionary<string, string> MetaData { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public string CRC { get; set; }
        public byte[] FileMain { get; set; }
        public byte[] FileManifest { get; set; }
    }
}