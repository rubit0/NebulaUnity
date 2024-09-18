using System.Collections.Generic;

namespace Core.API.Dtos.Requests
{
    public class UploadAssetBundleDto
    {
        public string BundleName { get; set; }
        public List<string> Dependencies { get; set; }
        public string CRC { get; set; }
        public byte[] FileMain { get; set; }
        public byte[] FileManifest { get; set; }
    }
}