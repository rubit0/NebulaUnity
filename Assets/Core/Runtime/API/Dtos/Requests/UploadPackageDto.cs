namespace Nebula.Core.Runtime.API.Dtos.Requests
{
    public class UploadPackageDto
    {
        public byte[] FileMain { get; set; }
        public string PackagePlatform { get; set; }
    }
}