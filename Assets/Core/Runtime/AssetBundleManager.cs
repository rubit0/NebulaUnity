using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.API;
using Core.Misc;
using UnityEngine;

namespace Core.Runtime
{
    public class AssetBundleManager
    {
        public IReadOnlyList<string> AvailableAssetBundles { get; private set; }
        public IReadOnlyList<AssetBundle> LoadedAssetBundles => _loadedAssetBundles;
    
        private readonly Dictionary<string, string[]> _resolvedBundleDependencies = new();
        private readonly Dictionary<string, AssetBundle> _loadedAssetBundlesDictionary = new();
        private readonly List<AssetBundle> _loadedAssetBundles = new ();
        
        private AssetsWebService _webService;
        private bool _didInit;

        public AssetBundleManager(NebulaSettings settings)
        {
            _webService = new AssetsWebService(settings.Endpoint);
        }
    
        public void Init()
        {
            if (_didInit)
            {
                return;
            }
            _didInit = true;
            
            AssetManagementUtils.InitAssetDataDirectory();
            if (AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                AvailableAssetBundles = new List<string>(0);
                Debug.Log("AssetBundles directory is empty, please sync from backend.");
                return;
            }
            
            var rootAssetBundle = AssetManagementUtils.LoadRootAssetBundle();
            var rootManifest = AssetManagementUtils.LoadRootManifest(rootAssetBundle);
            
            ResolveDependencies(rootManifest);
            rootAssetBundle.Unload(true);
        }
    
        private void ResolveDependencies(AssetBundleManifest rootManifest)
        {
            AvailableAssetBundles = rootManifest.GetAllAssetBundles().ToList();
            foreach (var bundleName in AvailableAssetBundles)
            {
                var dependencies = rootManifest.GetAllDependencies(bundleName);
                _resolvedBundleDependencies.Add(bundleName, dependencies);
            }
        }

        public AssetBundle LoadAssetBundle(string bundleName)
        {
            if (_loadedAssetBundlesDictionary.ContainsKey(bundleName))
            {
                return _loadedAssetBundlesDictionary[bundleName];
            }
        
            // Check for dependencies
            if (_resolvedBundleDependencies.ContainsKey(bundleName) 
                && _resolvedBundleDependencies[bundleName].Length > 0)
            {
                // Resolve dependencies before hand
                foreach (var dependency in _resolvedBundleDependencies[bundleName])
                {
                    if (_loadedAssetBundlesDictionary.ContainsKey(dependency))
                    {
                        continue;
                    }
                
                    var dependencyBundle = AssetManagementUtils.LoadBundle(dependency);
                    _loadedAssetBundlesDictionary.Add(dependency, dependencyBundle);
                    _loadedAssetBundles.Add(dependencyBundle);
                }
            }
        
            // Load bundle
            var bundle = AssetManagementUtils.LoadBundle(bundleName);
            _loadedAssetBundlesDictionary.Add(bundleName, bundle);
            _loadedAssetBundles.Add(bundle);

            return bundle;
        }
    
        public async Task<bool> DownloadRemoteAssetBundle(string bundleName, string remoteResourceUrl)
        {
            // Download asset bundle
            var downloadedBundle = await AssetManagementUtils.DownloadFile(remoteResourceUrl);
            if (!downloadedBundle.IsSuccess)
            {
                return false;
            }
            await File.WriteAllBytesAsync(AssetManagementUtils.GetAssetBundlePath() + $"/{bundleName}", downloadedBundle.Content);
        
            // Download manifest
            var downloadedManifest = await AssetManagementUtils.DownloadFile(remoteResourceUrl + ".manifest");
            if (!downloadedBundle.IsSuccess)
            {
                return false;
            }
            await File.WriteAllBytesAsync(AssetManagementUtils.GetAssetBundlePath() + $"/{bundleName}.manifest", downloadedManifest.Content);

            return true;
        }
    
        public void InstantiateAllGameObjects(string bundleName)
        {
            var bundle = LoadAssetBundle(bundleName);
        
            var gameObjects = bundle.LoadAllAssets<GameObject>();
            foreach (var prefab in gameObjects)
            {
                GameObject.Instantiate(prefab);
            }
        }
    }
}