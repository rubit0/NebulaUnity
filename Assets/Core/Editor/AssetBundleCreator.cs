using System.IO;
using System.Linq;
using Core.API;
using Core.API.Dtos.Requests;
using Core.Misc;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Core.Editor
{
    public static class AssetBundleCreator
    {
        [MenuItem("Nebula/Build AssetBundles")]
        public static void BuildAssetBundles()
        {
            // Create base asset folder if not exists and get reference
            AssetManagementUtils.InitAssetDataDirectory();
            if (AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                // Perform initial bundles build
                BuildPipeline.BuildAssetBundles(AssetManagementUtils.GetAssetBundlePath(), 
                    BuildAssetBundleOptions.None, 
                    BuildTarget.StandaloneWindows);
                
                Debug.Log("Performed initial AssetsBundle build");
            }
            else
            {
                PerformIncrementalBuild();
            }
        }

        private static void PerformIncrementalBuild()
        {
            Debug.Log("Performing incremental assets bundle build");

            // get current bundle manifest w/ hashes to compare with rebuild manifest
            var currentBundleManifest = AssetManagementUtils.LoadManifest();
            var currentAssetBundles = currentBundleManifest.GetAllAssetBundles();
            var currentHashes = currentAssetBundles
                .ToDictionary(k => k, v => currentBundleManifest.GetAssetBundleHash(v));
        
            // Build bundles
            var rebuildBundleManifest = BuildPipeline.BuildAssetBundles(AssetManagementUtils.GetAssetBundlePath(), 
                BuildAssetBundleOptions.None, 
                BuildTarget.StandaloneWindows);
        
            // Compare hash with backend to see what to upload/replace
            foreach (var assetBundleName in rebuildBundleManifest.GetAllAssetBundles())
            {
                var hash = rebuildBundleManifest.GetAssetBundleHash(assetBundleName);
                if (currentHashes.ContainsKey(assetBundleName))
                {
                    // check if old hash has changed
                    var oldHash = currentHashes[assetBundleName];
                    if (hash.CompareTo(oldHash) < 0)
                    {
                        Debug.Log(assetBundleName + " has changed and needs to be uploaded");
                    }
                }
                Debug.Log(assetBundleName + ": " + hash);
            }
        }
    
        [MenuItem("Nebula/Sync AssetBundles")]
        public static async void FetchAssetBundles()
        {
            //TODO fetch from backend remote manifest for comparison

            var client = new AssetsWebService("http://localhost:5280");
            var allBucketsResponse = await client.GetAllBuckets();
            if (!allBucketsResponse.IsSuccess)
            {
                return;
            }

            var bucketSimple = allBucketsResponse.Content[0];
            var bucketResponse = await client.GetBucket(bucketSimple.Id);
            if (!bucketResponse.IsSuccess)
            {
                return;
            }

            var bucket = bucketResponse.Content;
            
            AssetManagementUtils.InitAssetDataDirectory();
            AssetBundle.UnloadAllAssetBundles(true);
            var currentBundleManifest = AssetManagementUtils.LoadManifest();
            if (currentBundleManifest == null)
            {
                return;
            }
            
            // Compare hash with backend to see what to upload/replace
            foreach (var assetBundleName in currentBundleManifest.GetAllAssetBundles())
            {
                var hash = currentBundleManifest.GetAssetBundleHash(assetBundleName);

                var match = bucket.AssetBundles.SingleOrDefault(ab => ab.BundleName == assetBundleName);
                if (match == null)
                {
                    // New asset
                    var uploadRequest = new UploadAssetBundleDto
                    {
                        BundleName = assetBundleName,
                        CRC = hash.ToString(),
                        FileMain = File.ReadAllBytes(AssetManagementUtils.GetAssetBundlePath() + $"/{assetBundleName}"),
                        FileManifest = File.ReadAllBytes(AssetManagementUtils.GetAssetBundlePath() + $"/{assetBundleName}.manifest"),
                        FileRoot = File.ReadAllBytes(AssetManagementUtils.GetAssetBundlePath() + "/AssetBundles"),
                        FileRootManifest = File.ReadAllBytes(AssetManagementUtils.GetAssetBundlePath() + "/AssetBundles.manifest"),
                    };

                    var responseUpload = await client.UploadNewAssetBundle(bucket.Id, uploadRequest);
                    if (responseUpload.IsSuccess)
                    {
                        bucket.AssetBundles.Add(responseUpload.Content);
                    }
                }
                else
                {
                    Debug.Log(assetBundleName + ": " + hash);
                }
            }
        }
        
        [MenuItem("Nebula/Clear AssetBundles")]
        public static void ClearAssetBundles()
        {
            // Check if it exits and its not empty
            if (!AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                var path = AssetManagementUtils.GetAssetBundlePath();
                // Delete directory recursive
                Directory.Delete(path, true);
                
                Debug.Log("Deleted AssetBundles from " + path);
            }
        }
    }
}
#endif
