namespace Nebula.Runtime.API.Dtos.Requests
{
    public class DeleteAssetBundleDto
    {
        public string CRCRoot { get; set; }
        public byte[] FileRoot { get; set; }
        public byte[] FileRootManifest { get; set; }
    }
}