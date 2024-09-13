namespace Core.API.Dtos.Requests
{
    public class CreateBucketDto
    {
        public string Name { get; set; }
        public byte[] FileRoot { get; set; }
        public byte[] FileRootManifest { get; set; }
    }
}
