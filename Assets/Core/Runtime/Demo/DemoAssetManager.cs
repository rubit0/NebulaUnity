using Core.Misc;
using UnityEngine;

namespace Core.Runtime.Demo
{
    public class DemoAssetManager : MonoBehaviour
    {
        [SerializeField]
        private NebulaSettings settings;
        [SerializeField]
        private Transform rootBundlesList;
        [SerializeField]
        private ListItemAssetBundle listItemBundlePrefab;

        private AssetBundleManager _assetBundleManager;

        private void Start()
        {
            _assetBundleManager = new AssetBundleManager(settings);
            _assetBundleManager.Init();
            foreach (var availableAssetBundle in _assetBundleManager.AvailableAssetBundles)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(availableAssetBundle);
            }
        }
    }
}
