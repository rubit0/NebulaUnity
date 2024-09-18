using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.API;
using Core.Misc;
using UnityEngine;

namespace Core.Runtime
{
    public class AssetBundleManager
    {
        public IReadOnlyList<AssetBundle> LoadedAssetBundles => _loadedAssetBundles;
        public IReadOnlyList<AssetBundleInfo> LocalAssetBundles => _assetsIndex.Bundles;
        public IReadOnlyList<AssetBundleInfo> RemoteAssetBundles { get; private set; }
    
        private readonly Dictionary<string, string[]> _resolvedBundleDependencies = new ();
        private readonly Dictionary<string, AssetBundle> _loadedAssetBundlesDictionary = new ();
        private readonly List<AssetBundle> _loadedAssetBundles = new ();

        private NebulaSettings _settings;
        private AssetsWebService _client;
        private AssetsIndex _assetsIndex;
        private bool _didInit;

        public AssetBundleManager(NebulaSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.BucketId))
            {
                throw new Exception("No bucket id specified which is required");
            }

            _settings = settings;
            _client = new AssetsWebService(_settings.Endpoint);
            RemoteAssetBundles = new List<AssetBundleInfo>();
        }
    
        public async Task Init()
        {
            if (_didInit)
            {
                return;
            }
            _didInit = true;
            
            _assetsIndex = await AssetManagementUtils.LoadAssetsIndex();
            
            // Check if it is first run
            AssetManagementUtils.InitAssetDataDirectory();
            if (AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                Debug.Log("AssetBundles directory is empty, performing initial fetch from backend.");
                await Fetch();
            }
            
            ResolveDependencies();
        }

        /// <summary>
        /// Generate a report on which assets are updated and remaining to load from backend
        /// </summary>
        /// <returns>Assets report</returns>
        public AssetsComparisonReport ReportAssets()
        {
            var report = new AssetsComparisonReport();
            // Return empty if no remote asset bundle is present
            if (RemoteAssetBundles.Count == 0)
            {
                return report;
            }
            
            // Check for assets that are up to date or have update available
            foreach (var localBundle in _assetsIndex.Bundles)
            {
                var remote = RemoteAssetBundles.
                    SingleOrDefault(rab => rab.BundleName == localBundle.BundleName);
                if (remote == null)
                {
                    Debug.LogWarning($"The AssetBundle {localBundle.BundleName} doesn't seem to exist in the backend");
                    continue;
                }

                // Use CRC comparison to check for update
                if (localBundle.CRC == remote.CRC)
                {
                    report.UpToDate.Add(localBundle);
                }
                else
                {
                    report.Updated.Add(remote);
                }
            }
            
            // Check for remaining
            foreach (var remoteAssetBundle in RemoteAssetBundles)
            {
                // Skip if it exists locally
                if (_assetsIndex.Bundles
                    .Any(lab => lab.BundleName == remoteAssetBundle.BundleName))
                {
                    continue;
                }
                
                report.Remaining.Add(remoteAssetBundle);
            }

            return report;
        }

        /// <summary>
        /// Fetch remote asset info and update local data.
        /// This will not download any assets.
        /// </summary>
        public async Task Fetch()
        {
            Debug.Log("Performing fetching asset data from backend");
            
            // Fetch bucket details
            Debug.Log("Fetching remote bucket data");
            var client = new AssetsWebService(_settings.Endpoint);
            var bucketResponse = await client.GetBucket(_settings.BucketId);
            if (!bucketResponse.IsSuccess)
            {
                Debug.LogError($"Could not fetch data for bucket {_settings.BucketId}");
                return;
            }
            var bucketFromBackend = bucketResponse.Content;
            
            // If CRC was never set then it is an initial Fetch
            if (string.IsNullOrWhiteSpace(_assetsIndex.CRC))
            {
                // Init local index
                _assetsIndex.BucketId = _settings.BucketId;
                _assetsIndex.DataUrl = bucketFromBackend.DataUrl;
                _assetsIndex.ManifestUrl = bucketFromBackend.ManifestUrl;
                _assetsIndex.CRC = bucketFromBackend.CRC;
                _assetsIndex.Timestamp = bucketFromBackend.Timestamp;
                await AssetManagementUtils.StoreAssetsIndex(_assetsIndex);
                
                // Fetch root AssetBundle
                Debug.Log("Downloading remote root AssetsBundles");
                await AssetManagementUtils.DownloadAssetBundle("AssetBundles", 
                    _assetsIndex.DataUrl, 
                    _assetsIndex.ManifestUrl);
            }
            // Check if remote bucket changed and update local index
            else if (_assetsIndex.CRC != bucketFromBackend.CRC)
            {
                Debug.Log("Remote bucket is newer");
                _assetsIndex.CRC = bucketFromBackend.CRC;
                _assetsIndex.Timestamp = bucketFromBackend.Timestamp;
                await AssetManagementUtils.StoreAssetsIndex(_assetsIndex);

                // Fetch root AssetBundle since remote CRC changed
                Debug.Log("Downloading updated root AssetsBundles");
                await AssetManagementUtils.DownloadAssetBundle("AssetBundles", 
                    _assetsIndex.DataUrl, 
                    _assetsIndex.ManifestUrl);
            }
            
            var remoteAssetBundles = new List<AssetBundleInfo>();
            // Fetch remote AssetBundles
            foreach (var assetBundle in bucketFromBackend.AssetBundles)
            {
                Debug.Log($"Fetching {assetBundle.Id} AssetsBundle meta");
                var assetBundleInfo = new AssetBundleInfo
                {
                    BundleName = assetBundle.Id,
                    DisplayName = assetBundle.DisplayName,
                    Version = assetBundle.Version,
                    CRC = assetBundle.CRC,
                    DataUrl = assetBundle.DataUrl,
                    ManifestUrl = assetBundle.ManifestUrl,
                    Timestamp = assetBundle.Timestamp
                };

                remoteAssetBundles.Add(assetBundleInfo);
            }
            RemoteAssetBundles = remoteAssetBundles;
            
            Debug.Log("Fetch completed");
        }
        
        /// <summary>
        /// This will perform a complete data sync with the backend 
        /// </summary>
        public async Task Sync()
        {
            if (!_didInit)
            {
                throw new Exception("You must init before initiating a sync");
            }
            
            Debug.Log("Starting sync with backend.");

            // Fetch bucket details
            Debug.Log("Fetching remote bucket data");
            var bucketResponse = await _client.GetBucket(_settings.BucketId);
            if (!bucketResponse.IsSuccess)
            {
                Debug.LogError($"Could not fetch data for bucket {_settings.BucketId}");
                return;
            }
            var bucketFromBackend = bucketResponse.Content;
            
            // Check if it is very first init
            AssetManagementUtils.InitAssetDataDirectory();
            if (AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                Debug.Log("No local AssetBundles found, fetching all from backend.");
             
                // Init local index
                _assetsIndex.BucketId = _settings.BucketId;
                _assetsIndex.DataUrl = bucketFromBackend.DataUrl;
                _assetsIndex.ManifestUrl = bucketFromBackend.ManifestUrl;
                _assetsIndex.CRC = bucketFromBackend.CRC;
                _assetsIndex.Timestamp = bucketFromBackend.Timestamp;
                
                // Fetch root AssetBundle
                Debug.Log("Downloading root AssetsBundles");
                await AssetManagementUtils.DownloadAssetBundle("AssetBundles", 
                    _assetsIndex.DataUrl, 
                    _assetsIndex.ManifestUrl);

                // Fetch AssetBundles
                foreach (var assetBundle in bucketFromBackend.AssetBundles)
                {
                    Debug.Log($"Downloading {assetBundle.Id} AssetsBundle");
                    var assetBundleInfo = new AssetBundleInfo
                    {
                        BundleName = assetBundle.Id,
                        DisplayName = assetBundle.DisplayName,
                        Version = assetBundle.Version,
                        CRC = assetBundle.CRC,
                        DataUrl = assetBundle.DataUrl,
                        ManifestUrl = assetBundle.ManifestUrl,
                        Timestamp = assetBundle.Timestamp
                    };
                    _assetsIndex.Bundles.Add(assetBundleInfo);
                    await AssetManagementUtils.DownloadAssetBundle(assetBundleInfo.BundleName, 
                        assetBundleInfo.DataUrl,
                        assetBundleInfo.ManifestUrl);
                }

                // Store local index
                await AssetManagementUtils.StoreAssetsIndex(_assetsIndex);
                Debug.Log("Sync from backend completed");
                return;
            }

            // Check if remote bucket changed and update local index
            Debug.Log("Remote bucket is newer");
            if (_assetsIndex.CRC != bucketFromBackend.CRC)
            {
                _assetsIndex.CRC = bucketFromBackend.CRC;
                _assetsIndex.Timestamp = bucketFromBackend.Timestamp;
            }
            
            // Get all bundles that need to be downloaded
            var bundlesToDownload = bucketFromBackend.AssetBundles
                .Where(backend => _assetsIndex.Bundles.All(local => local.BundleName != backend.Id))
                .ToList();
            foreach (var assetToDownload in bundlesToDownload)
            {
                Debug.Log($"Downloading new {assetToDownload.Id} AssetsBundle");
                var assetBundleInfo = new AssetBundleInfo
                {
                    BundleName = assetToDownload.Id,
                    DisplayName = assetToDownload.DisplayName,
                    Version = assetToDownload.Version,
                    CRC = assetToDownload.CRC,
                    DataUrl = assetToDownload.DataUrl,
                    ManifestUrl = assetToDownload.ManifestUrl,
                    Timestamp = assetToDownload.Timestamp
                };
                _assetsIndex.Bundles.Add(assetBundleInfo);
                await AssetManagementUtils.DownloadAssetBundle(assetBundleInfo.BundleName, 
                    assetBundleInfo.DataUrl,
                    assetBundleInfo.ManifestUrl);
            }
            
            // Get all assets that have been changed on the backend
            var changedAssets = bucketFromBackend.AssetBundles
                .Join(_assetsIndex.Bundles, backend => backend.Id, local => local.BundleName, (backend, local) => new { Backend = backend, Local = local })
                .Where(x => x.Backend.CRC != x.Local.CRC)
                .Select(x => x.Backend)
                .ToList();
            foreach (var changedAsset in changedAssets)
            {
                Debug.Log($"Downloading updated {changedAsset.Id} AssetsBundle");
                var localBundleToUpdate = _assetsIndex.Bundles.Single(b => b.BundleName == changedAsset.Id);
                localBundleToUpdate.Version = changedAsset.Version;
                localBundleToUpdate.CRC = changedAsset.CRC;
                localBundleToUpdate.Timestamp = changedAsset.Timestamp;
                
                await AssetManagementUtils.DownloadAssetBundle(localBundleToUpdate.BundleName, 
                    localBundleToUpdate.DataUrl,
                    localBundleToUpdate.ManifestUrl);
            }

            // Update local index and local root AssetBundle
            if (bundlesToDownload.Any() || changedAssets.Any())
            {
                // Store index
                await AssetManagementUtils.StoreAssetsIndex(_assetsIndex);
                
                // Fetch/update root AssetBundle
                await AssetManagementUtils.DownloadAssetBundle("AssetBundles", 
                    _assetsIndex.DataUrl, 
                    _assetsIndex.ManifestUrl);
            }
            
            Debug.Log("Sync from backend completed");
        }
    
        private void ResolveDependencies()
        {
            var rootAssetBundle = AssetManagementUtils.LoadRootAssetBundle();
            var rootManifest = AssetManagementUtils.LoadRootManifest(rootAssetBundle);
            foreach (var bundleInfo in LocalAssetBundles)
            {
                var dependencies = rootManifest.GetAllDependencies(bundleInfo.BundleName);
                _resolvedBundleDependencies.Add(bundleInfo.BundleName, dependencies);
            }
            rootAssetBundle.Unload(true);
        }

        /// <summary>
        /// Load an AssetBundle with its dependencies.
        /// </summary>
        /// <param name="bundleName">Name of the AssetBundle to load.</param>
        /// <returns>The activated/loaded AssetBundle</returns>
        public AssetBundle LoadAssetBundle(string bundleName)
        {
            if (_loadedAssetBundlesDictionary.TryGetValue(bundleName, out var assetBundle))
            {
                return assetBundle;
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

        public void UnloadAssetBundle(AssetBundle assetBundle, bool unloadDependencies)
        {
            if (!_loadedAssetBundlesDictionary.ContainsKey(assetBundle.name))
            {
                Debug.LogWarning($"The AssetBundle {assetBundle.name} is not loaded.");
                return;
            }

            if (unloadDependencies)
            {
                // Unload dependencies
                foreach (var dependency in _resolvedBundleDependencies[assetBundle.name])
                {
                    if (!_loadedAssetBundlesDictionary.ContainsKey(dependency))
                    {
                        continue;
                    }

                    var dependencyBundle = _loadedAssetBundlesDictionary[dependency];
                    dependencyBundle.Unload(true);
                    _loadedAssetBundles.Remove(dependencyBundle);
                }
            }
            
            // Unload bundle
            assetBundle.Unload(true);
            _loadedAssetBundles.Remove(assetBundle);
            _loadedAssetBundlesDictionary.Remove(assetBundle.name);
        }
        
        /// <summary>
        /// Download an remote AssetBundle and replace if already existing
        /// </summary>
        /// <param name="assetBundle">AssetBundle to download</param>
        /// <returns>Success indicator</returns>
        public async Task<bool> DownloadRemoteAssetBundle(AssetBundleInfo assetBundle)
        {
            // Download asset bundle
            var downloadRequest = await AssetManagementUtils.DownloadAssetBundle(
                assetBundle.BundleName, assetBundle.ManifestUrl, assetBundle.ManifestUrl);
            if (!downloadRequest)
            {
                return false;
            }

            // Update index
            var assetInfoFromIndex = _assetsIndex.Bundles
                .SingleOrDefault(b => b.BundleName == assetBundle.BundleName);
            if (assetInfoFromIndex != null)
            {
                _assetsIndex.Bundles.Remove(assetInfoFromIndex);
            }
            _assetsIndex.Bundles.Add(assetBundle);
            await AssetManagementUtils.StoreAssetsIndex(_assetsIndex);

            return true;
        }
    
        /// <summary>
        /// Load an AssetBundle and instantiate all GameObjects
        /// </summary>
        /// <param name="bundleName">The name of the AssetBundle to load</param>
        /// <returns>All GameObject instances from the Bundle</returns>
        public List<GameObject> LoadAndInstantiateAll(string bundleName)
        {
            var bundle = LoadAssetBundle(bundleName);

            var instances = new List<GameObject>();
            var gameObjects = bundle.LoadAllAssets<GameObject>();
            foreach (var prefab in gameObjects)
            {
                var instance = GameObject.Instantiate(prefab);
                instances.Add(instance);
            }

            return instances;
        }
    }
}