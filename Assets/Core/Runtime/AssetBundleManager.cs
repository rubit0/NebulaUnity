using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Misc;
using UnityEngine;

namespace Core.Runtime
{
    public class AssetBundleManager
    {
        public IReadOnlyList<AssetBundle> LoadedAssetBundles => _loadedAssetBundles;
    
        private readonly Dictionary<string, string[]> _resolvedBundleDependencies = new();
        private readonly Dictionary<string, AssetBundle> _loadedAssetBundlesDictionary = new();
        private readonly List<AssetBundle> _loadedAssetBundles = new ();
    
        public void Init()
        {
            var rootManifest = AssetManagementUtils.LoadManifest();
            ResolveDependencies(rootManifest);
        }
    
        private void ResolveDependencies(AssetBundleManifest rootManifest)
        {
            var allBundles = rootManifest.GetAllAssetBundles();
            foreach (var bundleName in allBundles)
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
            if (_resolvedBundleDependencies[bundleName].Length > 0)
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