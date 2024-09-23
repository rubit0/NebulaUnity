namespace Nebula.Runtime.API.Dtos.Requests
{
    public class UpdateRootAssetBundleDto
    {
        public string CRC { get; set; }
        public byte[] FileMain { get; set; }
        public byte[] FileManifest { get; set; }
    }
}