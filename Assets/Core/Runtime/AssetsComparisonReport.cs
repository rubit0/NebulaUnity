using System.Collections.Generic;

namespace Core.Runtime
{
    public class AssetsComparisonReport
    {
        public List<AssetBundleInfo> UpToDate { get; set; } = new();
        public List<AssetBundleInfo> Updated { get; set; } = new();
        public List<AssetBundleInfo> Remaining { get; set; } = new();
    }
}