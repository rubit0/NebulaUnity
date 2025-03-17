using System.Collections.Generic;

namespace Nebula.Runtime
{
    public class AssetsIndex
    {
        /// <summary>
        /// Localy stored asset
        /// </summary>
        public List<Asset> Assets { get; set; } = new();
    }
}