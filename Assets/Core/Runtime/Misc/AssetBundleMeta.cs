using UnityEngine;

namespace Nebula.Runtime.Misc
{
    [CreateAssetMenu(menuName = "Nebula/Create Bundle Meta")]
    public class AssetBundleMeta : ScriptableObject
    {
        [field: SerializeField]
        public string DisplayName { get; set; } = "Bundle Name";

        [field: SerializeField]
        public string AssetType { get; set; } = "Misc";
    }
}