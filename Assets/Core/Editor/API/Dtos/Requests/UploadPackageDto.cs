namespace Nebula.Editor.API.Dtos.Requests
{
    public class UploadPackageDto
    {
        public byte[] FileMain { get; set; }
        public string PackagePlatform { get; set; }
    }
}