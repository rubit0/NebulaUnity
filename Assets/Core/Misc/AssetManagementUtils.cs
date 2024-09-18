using System.IO;
using System.Threading.Tasks;
using Core.API;
using Core.Runtime;
using Newtonsoft.Json;
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
        public static AssetBundleManifest LoadRootManifest(AssetBundle assetBundle)
        {
            return assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }
        
        /// <summary>
        /// Load the root AssetBundle from local storage
        /// </summary>
        public static AssetBundle LoadRootAssetBundle()
        {
            return LoadBundle("AssetBundles");
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
        /// Locally store the AssetsIndex
        /// </summary>
        public static async Task StoreAssetsIndex(AssetsIndex index)
        {
            var path = Path.Combine(GetAssetBundlePath(), "index.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var json = JsonConvert.SerializeObject(index);
            await File.WriteAllTextAsync(path, json);
        }

        /// <summary>
        /// Load the local AssetsIndex
        /// </summary>
        public static async Task<AssetsIndex> LoadAssetsIndex()
        {
            var path = Path.Combine(GetAssetBundlePath(), "index.json");
            if (!File.Exists(path))
            {
                return new AssetsIndex();
            }

            var json = await File.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<AssetsIndex>(json);
        }
        
        /// <summary>
        /// Download a remote AssetBundle and overwrite if locally already present 
        /// </summary>
        /// <param name="assetName">Name of the AssetBundle</param>
        /// <param name="bundleUrl">Url to the remote AssetBundle</param>
        /// <param name="manifestUrl"></param>
        /// <returns></returns>
        public static async Task<bool> DownloadAssetBundle(string assetName, string bundleUrl, string manifestUrl)
        {
            Debug.Log($"Downloading [{assetName}] AssetBundle from {bundleUrl}");
            var mainBundleResponse = await DownloadFile(bundleUrl);
            if (!mainBundleResponse.IsSuccess)
            {
                Debug.LogError($"Could not load root AssetBundle from {bundleUrl}");
                return false;
            }

            var assetBundlesPath = Path.Combine(GetAssetBundlePath(), assetName);
            if (File.Exists(assetBundlesPath))
            {
                File.Delete(assetBundlesPath);
            }
            await File.WriteAllBytesAsync(Path.Combine(GetAssetBundlePath(), assetName), mainBundleResponse.Content);
            
            Debug.Log($"Downloading [{assetName}] manifest from {manifestUrl}");
            var manifestBundleResponse = await DownloadFile(manifestUrl);
            if (!manifestBundleResponse.IsSuccess)
            {
                Debug.LogError($"Could not load root AssetBundle from {manifestUrl}");
                return false;
            }

            var manifestPath = Path.Combine(GetAssetBundlePath(), $"{assetName}.manifest");
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }
            await File.WriteAllBytesAsync(Path.Combine(GetAssetBundlePath(), $"{assetName}.manifest"), manifestBundleResponse.Content);
            
            return true;
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