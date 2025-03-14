using System.Collections.Generic;

namespace Nebula.Runtime
{
    public class AssetsIndex
    {
        /// <summary>
        /// Local asset releases
        /// </summary>
        public List<LocalAsset> Assets { get; set; } = new();
    }
}