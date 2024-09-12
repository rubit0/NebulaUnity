using System.IO;
using System.Threading.Tasks;
using Core.API;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Misc
{
    public static class AssetManagementUtils
    {
        private static bool _didGetAssetBundlePath;
        private static string _assetBundlePath;
    
        /// <summary>
        /// Get the local asset bundle storage folder for the current environment
        /// </summary>
        public static string GetAssetBundlePath()
        {
            if (!_didGetAssetBundlePath)
            {
                _assetBundlePath = Path.Combine(
                    Path.GetDirectoryName(Application.isEditor 
                        ? Application.dataPath 
                        : Application.persistentDataPath), 
                    "AssetBundles");
            
                _didGetAssetBundlePath = true;
            }

            return _assetBundlePath;
        }
    
        /// <summary>
        /// Init the local storage folder on where to store and save AssetBundles from
        /// </summary>
        public static void InitAssetDataDirectory()
        {
            var path = GetAssetBundlePath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        /// <summary>
        /// Check if the current data directory is empty
        /// </summary>
        public static bool IsAssetDataDirectoryEmpty()
        {
            var path = GetAssetBundlePath();
            if (!Directory.Exists(path))
            {
                return false;
            }

            return Directory.GetFiles(path).Length == 0;
        }
    
        /// <summary>
        /// Load manifest from local storage
        /// </summary>
        public static AssetBundleManifest LoadManifest()
        {
            var mainAssetBundle = LoadBundle("AssetBundles");
            return mainAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }
    
        /// <summary>
        /// Load an AssetBundle from the local storage
        /// </summary>
        /// <param name="bundleName">Key name of the AssetBundle</param>
        public static AssetBundle LoadBundle(string bundleName)
        {
            return AssetBundle.LoadFromFile(Path.Combine(GetAssetBundlePath(), bundleName));
        }
    
        /// <summary>
        /// Download raw binary file from a resource
        /// </summary>
        /// <param name="url">url to load the resource from</param>
        public static Task<WebResponse<byte[]>> DownloadFile(string url)
        {
            var completionSource = new TaskCompletionSource<WebResponse<byte[]>>();
            var request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<byte[]>.Failed(request.error));
                }
                else
                {
                    completionSource.SetResult(WebResponse<byte[]>.Success(request.downloadHandler.data));
                }
                request.Dispose();
            };
            
            return completionSource.Task;
        }
    }
}