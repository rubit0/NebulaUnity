using UnityEngine;

namespace Core.Runtime
{
    public class AssetBundleLoader : MonoBehaviour
    {
        public string remoteAssetBundleName;
        public string urlForRemoteAsset;
        public string assetBundleToLoad;
        private AssetBundleManager _assetBundleManager;

        private async void Start()
        {
            _assetBundleManager = new AssetBundleManager();
            _assetBundleManager.Init();
            await _assetBundleManager.DownloadRemoteAssetBundle(remoteAssetBundleName, urlForRemoteAsset);
            _assetBundleManager.InstantiateAllGameObjects(assetBundleToLoad);
        }
    }
}
