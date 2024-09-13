namespace Core.API.Dtos.Requests
{
    public class UploadAssetBundleDto
    {
        public string BundleName { get; set; }
        public string CRC { get; set; }
        public byte[] FileRoot { get; set; }
        public byte[] FileRootManifest { get; set; }
        public byte[] FileMain { get; set; }
        public byte[] FileManifest { get; set; }
    }
}