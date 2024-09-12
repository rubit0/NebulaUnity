using System.IO;
using System.Linq;
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
        public static void FetchAssetBundles()
        {
            //TODO fetch from backend remote manifest for comparison
        
            // Create base asset folder if not exists and get reference
            AssetManagementUtils.InitAssetDataDirectory();
            var assetBundleDirectory = AssetManagementUtils.GetAssetBundlePath();
        
            // Load the manifest to get all bundles
            var bundleManifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory, 
                BuildAssetBundleOptions.None, 
                BuildTarget.StandaloneWindows);
        
            // Compare hash with backend to see what to upload/replace
            foreach (var assetBundleName in bundleManifest.GetAllAssetBundles())
            {
                var hash = bundleManifest.GetAssetBundleHash(assetBundleName);
                Debug.Log(assetBundleName + ": " + hash);
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
