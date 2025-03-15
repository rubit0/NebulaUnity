namespace Nebula.Shared.API.Dtos
{
    public class PackageDto
    {
        /// <summary>
        /// Url to the main blob of this package
        /// </summary>
        public string BlobUrl { get; set; }
        /// <summary>
        /// Target platform this package is supported on
        /// </summary>
        public string Platform { get; set; }
    }
}