using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nebula.Runtime.API;
using Nebula.Runtime.API.Dtos.Responses;
using Nebula.Runtime.Misc;
using UnityEngine;

namespace Nebula.Runtime
{
    public class AssetBundleManager
    {
        public IReadOnlyList<AssetBundle> LoadedAssetBundle => _loadedAssetBundlesDictionary.Values.ToList();
        public IReadOnlyList<LocalAsset> LocalAssetBundles => _assetsIndex.Assets;
        public IReadOnlyList<AssetContainerDto> RemoteAvailableAssets => _remoteAvailableAssets;
        public IReadOnlyList<AssetContainerDto> UpdateableAssets => _updateableAssets;

        private readonly Dictionary<string, AssetBundle> _loadedAssetBundlesDictionary = new ();
        private readonly List<AssetContainerDto> _remoteAvailableAssets = new();
        private readonly List<AssetContainerDto> _updateableAssets = new();
        private readonly NebulaSettings _settings;
        private readonly AssetsWebService _client;
        private AssetsIndex _assetsIndex;
        private bool _didInit;

        public AssetBundleManager(NebulaSettings settings)
        {
            _settings = settings;
            _client = new AssetsWebService(_settings.Endpoint);
        }
    
        /// <summary>
        /// Init the asset manager.
        /// </summary>
        public async Task Init()
        {
            if (_didInit)
            {
                return;
            }
            _didInit = true;
            
            // Check if it is first run
            await AssetManagementUtils.InitAssetContainersDirectory();
            _assetsIndex = await AssetManagementUtils.LoadAssetsIndex();

            if (_assetsIndex.Assets.Count == 0)
            {
                Debug.Log("AssetBundles directory is empty, performing initial fetch from backend.");
                await Fetch();
            }
        }

        /// <summary>
        /// Fetch info about available new assets and updates.
        /// </summary>
        public async Task Fetch()
        {
            Debug.Log("Performing fetching asset data from backend");
            
            // Fetch bucket details
            Debug.Log("Fetching remote bucket data");
            var client = new AssetsWebService(_settings.Endpoint);
            var containerResponse = await client.GetAllContainer();
            if (!containerResponse.IsSuccess)
            {
                Debug.LogError($"Could not fetch containers data from backend: {containerResponse.ErrorMessage}");
            }
            var assetContainers = containerResponse.Content;
            foreach (var assetContainerFromBackend in assetContainers)
            {
                var localAssetContainer =
                    _assetsIndex.Assets
                        .SingleOrDefault(ac => ac.Id == assetContainerFromBackend.Id);
                if (localAssetContainer == null)
                {
                    if (_remoteAvailableAssets.All(a => assetContainerFromBackend.Id != a.Id))
                    {
                        _remoteAvailableAssets.Add(assetContainerFromBackend);
                    }
                }
                else if (assetContainerFromBackend.Timestamp > localAssetContainer.Timestamp)
                {
                    _updateableAssets.Add(assetContainerFromBackend);
                }
            }
            
            Debug.Log("Fetch completed");
        }
        
        /// <summary>
        /// This will perform a complete data sync with the backend.
        /// </summary>
        public async Task Sync()
        {
            if (!_didInit)
            {
                throw new Exception("You must init before initiating a sync");
            }
            
            Debug.Log("Starting sync with backend.");
            await Fetch();
            
            // Download all new assets
            foreach (var remoteAsset in _remoteAvailableAssets.ToList())
            {
                var success = await DownloadAsset(remoteAsset);
                if (success)
                {
                    _remoteAvailableAssets.Remove(remoteAsset);
                }
            }
            
            // Update all assets
            foreach (var remoteAsset in _updateableAssets)
            {
                var success = await DownloadAsset(remoteAsset);
                if (success)
                {
                    _remoteAvailableAssets.Remove(remoteAsset);
                }
            }
            
            Debug.Log("Sync from backend completed");
        }

        /// <summary>
        /// Load an asset with its dependencies.
        /// </summary>
        /// <param name="container">Name of the AssetBundle to load.</param>
        /// <returns>The activated/loaded AssetBundle</returns>
        public async Task<AssetBundle> LoadAsset(LocalAsset container)
        {
            if (_loadedAssetBundlesDictionary.TryGetValue(container.Id, out var assetBundle))
            {
                return assetBundle;
            }
        
            // Load bundle
            var bundle = await AssetManagementUtils.LoadBundleAsync(container.Id);
            _loadedAssetBundlesDictionary.Add(container.Id, bundle);

            return bundle;
        }
        
        /// <summary>
        /// Load an AssetBundle and instantiate all GameObjects async
        /// </summary>
        /// <param name="container">The name of the local container to load from</param>
        /// <returns>All GameObject instances from the Bundle</returns>
        public async Task<List<GameObject>> LoadAndInstantiateAll(LocalAsset container)
        {
            var bundle = await LoadAsset(container);
            return await InstantiateAll(bundle);
        }
        
        /// <summary>
        /// Instantiate all GameObjects async from an AssetBundle.
        /// </summary>
        /// <param name="bundle">AssetBundle to load from</param>
        /// <returns>All GameObject instances from the Bundle</returns>
        public async Task<List<GameObject>> InstantiateAll(AssetBundle bundle)
        {
            var completionSource = new TaskCompletionSource<List<GameObject>>();
            var request = bundle.LoadAllAssetsAsync<GameObject>();
            request.completed += operation =>
            {
                var instances = new List<GameObject>();
                if (request.allAssets == null || request.allAssets.Length == 0)
                {
                    completionSource.SetResult(instances);
                    return;
                }
                
                var gameObjects = request.allAssets.Cast<GameObject>().ToList();
                foreach (var prefab in gameObjects)
                {
                    var instance = GameObject.Instantiate(prefab);
                    instances.Add(instance);
                    Debug.Log($"Instantiated {instance.name} GameObject from AssetBundle.");
                }
                
                completionSource.SetResult(instances);
            };

            return await completionSource.Task;
        }

        /// <summary>
        /// Unload an AssetBundle from memory.
        /// </summary>
        /// <param name="assetBundle">AssetBundle to unload from</param>
        public void UnloadAssetBundle(AssetBundle assetBundle)
        {
            if (!_loadedAssetBundlesDictionary.ContainsKey(assetBundle.name))
            {
                Debug.LogWarning($"The AssetBundle {assetBundle.name} is not loaded.");
                return;
            }

            // Unload bundle
            assetBundle.Unload(true);
            // _loadedAssetBundles.Remove(assetBundle);
            _loadedAssetBundlesDictionary.Remove(assetBundle.name);
        }
        
        /// <summary>
        /// Download latest release from a container and replace if already existing.
        /// Updates local index entry.
        /// </summary>
        /// <param name="assetContainerDto">AssetContainer to download latest release from</param>
        /// <returns>Success indicator</returns>
        public async Task<bool> DownloadAsset(AssetContainerDto assetContainerDto)
        {
            var releaseResponse = await _client.GetRelease(assetContainerDto.Id, 
                assetContainerDto.ReleaseSlotDev);
            if (!releaseResponse.IsSuccess)
            {
                Debug.LogError($"Failed to get release from remote asset bundle: {releaseResponse.ErrorMessage}");
                return false;
            }
            var release = releaseResponse.Content;
            var package = release.Packages.SingleOrDefault(p => p.Platform == RuntimePlatform.IPhonePlayer.ToString());
            if (package == null)
            {
                Debug.LogError(
                    $"No package found in remote asset bundle that matches the platform {Application.platform}");
                return false;
            }
            
            // Download asset bundle
            var downloadRequest = await AssetManagementUtils.DownloadAssetRelease(
                assetContainerDto.Id, 
                package.BlobUrl);
            if (!downloadRequest)
            {
                return false;
            }
            
            // Add or update entry in local index
            var localContainer = _assetsIndex.Assets
                .SingleOrDefault(ac => ac.Id == assetContainerDto.Id);
            if (localContainer == null)
            {
                var updatedContainerInfo = new LocalAsset
                {
                    Id = assetContainerDto.Id,
                    Timestamp = assetContainerDto.Timestamp,
                    Version = assetContainerDto.LatestReleaseVersion,
                    Name = assetContainerDto.Name,
                    Notes = release.Notes,
                    MetaData = assetContainerDto.Meta,
                };
                _assetsIndex.Assets.Add(updatedContainerInfo);
            }
            else
            {
                localContainer.Version = assetContainerDto.LatestReleaseVersion;
                localContainer.Timestamp = assetContainerDto.Timestamp;
                localContainer.Notes = release.Notes;
                localContainer.MetaData = assetContainerDto.Meta;
            }
            
            await AssetManagementUtils.StoreAssetsIndex(_assetsIndex);

            return true;
        }
    }
}