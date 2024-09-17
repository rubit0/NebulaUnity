using Core.Misc;
using UnityEngine;

namespace Core.Runtime
{
    public class AssetBundleLoader : MonoBehaviour
    {
        [SerializeField]
        private NebulaSettings settings;
        
        public string remoteAssetBundleName;
        public string urlForRemoteAsset;
        public string assetBundleToLoad;
        private AssetBundleManager _assetBundleManager;

        private void Start()
        {
            _assetBundleManager = new AssetBundleManager(settings);
            _assetBundleManager.Init();
            // await _assetBundleManager.DownloadRemoteAssetBundle(remoteAssetBundleName, urlForRemoteAsset);
            // _assetBundleManager.InstantiateAllGameObjects(assetBundleToLoad);
        }
    }
}
