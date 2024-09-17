using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.API;
using Core.API.Dtos.Requests;
using Core.Misc;
using Core.Runtime;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Core.Editor
{
    public static class AssetBundleCreator
    {
        [MenuItem("Nebula/Build & Push")]
        public static void PushAssetBundles()
        {
            var settings = GetSettings();
            // Create base asset folder if not exists and get reference
            AssetManagementUtils.InitAssetDataDirectory();
            if (AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                // Check if a bucketId already exists
                if (string.IsNullOrEmpty(settings.BucketId))
                {
                    Debug.Log("Local folder and no bucket defined, creating new initial build");
                    PerformInitialBuildWithBucket(settings);
                }
                else
                {
                    Debug.LogWarning("Local folder empty but a bucket is defined, performing sync instead.");
                    FetchAssetBundles();
                }
            }
            else
            {
                PerformIncrementalBuild(settings);
            }
        }

        private static async void PerformInitialBuildWithBucket(NebulaSettings settings)
        {
            Debug.Log("Performing AssetBundle build for a new bucket om backend");
            // Perform initial bundles build
            var rootBundleManifest = BuildPipeline.BuildAssetBundles(AssetManagementUtils.GetAssetBundlePath(), 
                BuildAssetBundleOptions.None, 
                BuildTarget.StandaloneWindows);
            
            Debug.Log("Creating a new bucket on backend");
            // Create a new bucket
            var client = new AssetsWebService(settings.Endpoint);
            var fileDataRootAssetBundle = await 
                File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), "AssetBundles"));
            var fileDataRootManifest = await 
                File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), "AssetBundles.manifest"));

            var createBucketDto = new CreateBucketDto
            {
                Name = "Test dev",
                CRC = GetCRCForBundle("AssetBundles"),
                FileRoot = fileDataRootAssetBundle,
                FileRootManifest = fileDataRootManifest
            };
            var response = await client.CreateNewAssetBucket(createBucketDto);
            if (!response.IsSuccess)
            {
                Debug.LogError($"Failed to create bucket, reason:\n{response.ErrorMessage}");
                return;
            }

            // Locally store in the settings the new bucket id
            var bucketFromBackend = response.Content;
            settings.BucketId = bucketFromBackend.Id;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            Debug.Log("Asset bucket created and Id set in local settings file");
            
            // Init local index
            var index = new AssetsIndex
            {
                BucketId = settings.BucketId,
                DataUrl = bucketFromBackend.DataUrl,
                ManifestUrl = bucketFromBackend.ManifestUrl,
                CRC = bucketFromBackend.CRC,
                Timestamp = bucketFromBackend.Timestamp
            };
            await AssetManagementUtils.StoreAssetsIndex(index);
            Debug.Log("Created and stored local index file");

            // Check for included bundles to upload
            var assetBundlesToUpload = rootBundleManifest.GetAllAssetBundles();
            Debug.Log("Found " + assetBundlesToUpload.Length + " AssetBundles to upload.");
            foreach (var assetBundleName in assetBundlesToUpload)
            {
                Debug.Log($"Uploading new AssetBundle [{assetBundleName}]");

                var crc = GetCRCForBundle(assetBundleName);
                var fileDataAssetBundle = await 
                    File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), assetBundleName));
                var fileDataManifest = await 
                    File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), assetBundleName + ".manifest"));
                
                var uploadBundleDto = new UploadAssetBundleDto
                {
                    BundleName = assetBundleName,
                    CRC = crc,
                    FileMain = fileDataAssetBundle,
                    FileManifest = fileDataManifest,
                };

                var uploadBundleResponse = await client.UploadNewAssetBundle(index.BucketId, uploadBundleDto);
                if (!uploadBundleResponse.IsSuccess)
                {
                    Debug.LogError($"Failed to upload AssetBundle [{assetBundleName}] reason:\n{uploadBundleResponse.ErrorMessage}");
                    continue;
                }

                var remoteAssetBundle = uploadBundleResponse.Content;
                var assetBundleInfo = new LocalAssetBundleInfo
                {
                    BundleName = remoteAssetBundle.Id,
                    DisplayName = remoteAssetBundle.DisplayName,
                    Version = remoteAssetBundle.Version,
                    CRC = remoteAssetBundle.CRC,
                    DataUrl = remoteAssetBundle.DataUrl,
                    ManifestUrl = remoteAssetBundle.ManifestUrl,
                    Timestamp = remoteAssetBundle.Timestamp
                };
                index.Bundles.Add(assetBundleInfo);
            }
            Debug.Log("Uploaded all initial AssetBundles");

            // update bucket since timestamp has changed due uploads
            var bucketResponse = await client.GetBucket(bucketFromBackend.Id);
            bucketFromBackend = bucketResponse.Content;
            index.Timestamp = bucketFromBackend.Timestamp;
            await AssetManagementUtils.StoreAssetsIndex(index);

            Debug.Log("Completed initial AssetsBundle build");
        }

        private static async void PerformIncrementalBuild(NebulaSettings settings)
        {
            // Steps> make build -> compare what has been added or updated
            
            Debug.Log("Performing AssetBundle build for a new bucket om backend");
            // Perform initial bundles build
            var rootBundleManifest = BuildPipeline.BuildAssetBundles(AssetManagementUtils.GetAssetBundlePath(), 
                BuildAssetBundleOptions.None, 
                BuildTarget.StandaloneWindows);
            
            // Compare CRC to check if things has been changed
            var index = await AssetManagementUtils.LoadAssetsIndex();
            var localCRC = GetCRCForBundle("AssetBundles");
            if (index.CRC == localCRC)
            {
                Debug.Log("No changed AssetBundles detected (matching CRC)");
                return;
            }

            // Set new CRC
            index.CRC = localCRC;
            
            // Get build bundles with CRC to compare
            var buildAssetBundles = rootBundleManifest.GetAllAssetBundles().ToDictionary(v => v, GetCRCForBundle);
            var updatedAssetBundlesToUpload = new List<LocalAssetBundleInfo>();
            foreach (var localAssetBundleInfo in index.Bundles)
            {
                if (!buildAssetBundles.ContainsKey(localAssetBundleInfo.BundleName))
                {
                    Debug.LogWarning($"It seems the bundle {localAssetBundleInfo.BundleName} has been deleted.?");
                }

                var localBundleCRC = buildAssetBundles[localAssetBundleInfo.BundleName];
                if (localAssetBundleInfo.CRC != localBundleCRC)
                {
                    Debug.Log($"The bundle {localAssetBundleInfo.BundleName} has been changed and will be uploaded.");
                    updatedAssetBundlesToUpload.Add(localAssetBundleInfo);
                }
            }
            
            // Perform uploads
            var client = new AssetsWebService(settings.Endpoint);

            Debug.Log($"Found {updatedAssetBundlesToUpload.Count} updated AssetBundles to upload.");
            // Upload all changed AssetBundles
            foreach (var localAssetBundleInfo in updatedAssetBundlesToUpload)
            {
                Debug.Log($"Uploading updated AssetBundle {localAssetBundleInfo.BundleName} to backend");
                var fileDataAssetBundle = await 
                    File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), localAssetBundleInfo.BundleName));
                var fileDataManifest = await 
                    File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), localAssetBundleInfo.BundleName + ".manifest"));
                
                var updateBundleDto = new UploadAssetBundleDto
                {
                    CRC = buildAssetBundles[localAssetBundleInfo.BundleName],
                    FileMain = fileDataAssetBundle,
                    FileManifest = fileDataManifest,
                    BundleName = localAssetBundleInfo.BundleName
                };

                var uploadBundleResponse = await client.UpdateAssetBundle(index.BucketId, localAssetBundleInfo.BundleName, updateBundleDto);
                if (!uploadBundleResponse.IsSuccess)
                {
                    Debug.LogError($"Could not upload new AssetBundle {localAssetBundleInfo.BundleName} reason:\n{uploadBundleResponse.ErrorMessage}");
                    continue;
                }

                var assetBundleDto = uploadBundleResponse.Content;
                localAssetBundleInfo.Timestamp = assetBundleDto.Timestamp;
                localAssetBundleInfo.Version = assetBundleDto.Version;
            }
            
            // Get newly added AssetBundles and upload
            var bundlesToUpload = buildAssetBundles.Keys
                .Where(fresh => index.Bundles.All(current => current.BundleName != fresh))
                .ToList();
            Debug.Log($"Found new {bundlesToUpload.Count} AssetBundles to upload.");
            foreach (var assetBundleName in bundlesToUpload)
            {
                Debug.Log($"Uploading new AssetBundle [{assetBundleName}]");
                var crc = GetCRCForBundle(assetBundleName);
                var fileDataAssetBundle = await 
                    File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), assetBundleName));
                var fileDataManifest = await 
                    File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), assetBundleName + ".manifest"));
                
                var uploadBundleDto = new UploadAssetBundleDto
                {
                    BundleName = assetBundleName,
                    CRC = crc,
                    FileMain = fileDataAssetBundle,
                    FileManifest = fileDataManifest,
                };

                var uploadBundleResponse = await client.UploadNewAssetBundle(index.BucketId, uploadBundleDto);
                if (!uploadBundleResponse.IsSuccess)
                {
                    Debug.LogError($"Failed to upload AssetBundle [{assetBundleName}] reason:\n{uploadBundleResponse.ErrorMessage}");
                    continue;
                }

                var remoteAssetBundle = uploadBundleResponse.Content;
                var assetBundleInfo = new LocalAssetBundleInfo
                {
                    BundleName = remoteAssetBundle.Id,
                    DisplayName = remoteAssetBundle.DisplayName,
                    Version = remoteAssetBundle.Version,
                    CRC = remoteAssetBundle.CRC,
                    DataUrl = remoteAssetBundle.DataUrl,
                    ManifestUrl = remoteAssetBundle.ManifestUrl,
                    Timestamp = remoteAssetBundle.Timestamp
                };
                index.Bundles.Add(assetBundleInfo);
            }
            
            // Upload root AssetBundle files
            var fileDataRootAssetBundle = await 
                File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), "AssetBundles"));
            var fileDataRootManifest = await 
                File.ReadAllBytesAsync(Path.Combine(AssetManagementUtils.GetAssetBundlePath(), "AssetBundles.manifest"));
            var updateRootAssetDto = new UpdateRootAssetBundleDto
            {
                CRC = localCRC,
                FileMain = fileDataRootAssetBundle,
                FileManifest = fileDataRootManifest
            };
            var updateRootAssetBundleResponse = await client.UpdateRootAssetBundle(index.BucketId, updateRootAssetDto);
            if (!updateRootAssetBundleResponse.IsSuccess)
            {
                Debug.LogError($"Failed to update the root AssetBundle, reason:\n{updateRootAssetBundleResponse.ErrorMessage}");
                return;
            }

            // Persist changes to local index
            index.Timestamp = updateRootAssetBundleResponse.Content.Timestamp;
            await AssetManagementUtils.StoreAssetsIndex(index);

            Debug.Log("Completed incremental assets bundle build with uploads");
        }
    
        [MenuItem("Nebula/Fetch")]
        public static async void FetchAssetBundles()
        {
            Debug.Log("Starting sync with backend.");
            var settings = GetSettings();
            if (string.IsNullOrEmpty(settings.BucketId))
            {
                Debug.LogError("No bucket id has been set!");
                return;
            }

            // Load local index
            var index = await AssetManagementUtils.LoadAssetsIndex();

            // Fetch bucket details
            Debug.Log("Fetching remote bucket data");
            var client = new AssetsWebService(settings.Endpoint);
            var bucketResponse = await client.GetBucket(settings.BucketId);
            if (!bucketResponse.IsSuccess)
            {
                Debug.LogError($"Could not fetch data for bucket {settings.BucketId}");
                return;
            }
            var bucketFromBackend = bucketResponse.Content;
            
            // Check if it is very first init
            AssetManagementUtils.InitAssetDataDirectory();
            if (AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                Debug.Log("No local AssetBundles found, fetching all from backend.");
             
                // Init local index
                index.BucketId = settings.BucketId;
                index.DataUrl = bucketFromBackend.DataUrl;
                index.ManifestUrl = bucketFromBackend.ManifestUrl;
                index.CRC = bucketFromBackend.CRC;
                index.Timestamp = bucketFromBackend.Timestamp;
                
                // Fetch root AssetBundle
                Debug.Log("Downloading root AssetsBundles");
                await AssetManagementUtils.DownloadAssetBundle("AssetBundles", 
                    index.DataUrl, 
                    index.ManifestUrl);
                
                // Fetch AssetBundles
                foreach (var assetBundle in bucketFromBackend.AssetBundles)
                {
                    Debug.Log($"Downloading {assetBundle.Id} AssetsBundle");
                    var assetBundleInfo = new LocalAssetBundleInfo
                    {
                        BundleName = assetBundle.Id,
                        DisplayName = assetBundle.DisplayName,
                        Version = assetBundle.Version,
                        CRC = assetBundle.CRC,
                        DataUrl = assetBundle.DataUrl,
                        ManifestUrl = assetBundle.ManifestUrl,
                        Timestamp = assetBundle.Timestamp
                    };
                    index.Bundles.Add(assetBundleInfo);
                    await AssetManagementUtils.DownloadAssetBundle(assetBundleInfo.BundleName, 
                        assetBundleInfo.DataUrl,
                        assetBundleInfo.ManifestUrl);
                }

                // Store local index
                await AssetManagementUtils.StoreAssetsIndex(index);
                Debug.Log("Sync from backend completed");
                return;
            }
            
            // Get all bundles that need to be downloaded
            var bundlesToDownload = bucketFromBackend.AssetBundles
                .Where(backend => index.Bundles.All(local => local.BundleName != backend.Id))
                .ToList();
            foreach (var assetToDownload in bundlesToDownload)
            {
                Debug.Log($"Downloading new {assetToDownload.Id} AssetsBundle");
                var assetBundleInfo = new LocalAssetBundleInfo
                {
                    BundleName = assetToDownload.Id,
                    DisplayName = assetToDownload.DisplayName,
                    Version = assetToDownload.Version,
                    CRC = assetToDownload.CRC,
                    DataUrl = assetToDownload.DataUrl,
                    ManifestUrl = assetToDownload.ManifestUrl,
                    Timestamp = assetToDownload.Timestamp
                };
                index.Bundles.Add(assetBundleInfo);
                await AssetManagementUtils.DownloadAssetBundle(assetBundleInfo.BundleName, 
                    assetBundleInfo.DataUrl,
                    assetBundleInfo.ManifestUrl);
            }
            
            // Get all assets that have been changed on the backend
            var changedAssets = bucketFromBackend.AssetBundles
                .Join(index.Bundles, backend => backend.Id, local => local.BundleName, (backend, local) => new { Backend = backend, Local = local })
                .Where(x => x.Backend.CRC != x.Local.CRC)
                .Select(x => x.Backend)
                .ToList();
            foreach (var changedAsset in changedAssets)
            {
                Debug.Log($"Downloading updated {changedAsset.Id} AssetsBundle");
                var localBundleToUpdate = index.Bundles.Single(b => b.BundleName == changedAsset.Id);
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
                await AssetManagementUtils.StoreAssetsIndex(index);
                
                // Fetch/update root AssetBundle
                await AssetManagementUtils.DownloadAssetBundle("AssetBundles", 
                    index.DataUrl, 
                    index.ManifestUrl);
            }
            
            Debug.Log("Sync from backend completed");
        }
        
        [MenuItem("Nebula/Clear All")]
        public static void ClearAssetBundles()
        {
            AssetBundle.UnloadAllAssetBundles(true);
            if (!Directory.Exists(AssetManagementUtils.GetAssetBundlePath()))
            {
                return;
            }
            
            // Check if it its not empty
            if (!AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                var path = AssetManagementUtils.GetAssetBundlePath();
                // Delete directory recursive
                Directory.Delete(path, true);
                
                Debug.Log("Deleted AssetBundles from " + path);
            }
        }
        
        private static NebulaSettings GetSettings()
        {
            var assets = AssetDatabase.FindAssets($"t:{typeof(NebulaSettings)}");
            if (!assets.Any())
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<NebulaSettings>(AssetDatabase.GUIDToAssetPath(assets[0]));
        }
        
        /// <summary>
        /// Get CRC as int from a local bundle
        /// </summary>
        /// <param name="bundleName">Name of the locally stored bundle</param>
        /// <returns>CRC as int</returns>
        private static string GetCRCForBundle(string bundleName)
        {
            uint hash = 0;
            BuildPipeline.GetCRCForAssetBundle(AssetManagementUtils.GetAssetBundlePath() + $"/{bundleName}", out hash);
            return hash.ToString();
        }
    }
}
#endif
