namespace Core.API.Dtos.Requests
{
    public class DeleteAssetBundleDto
    {
        public byte[] FileRoot { get; set; }
        public byte[] FileRootManifest { get; set; }
    }
}