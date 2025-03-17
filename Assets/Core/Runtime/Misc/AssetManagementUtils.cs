using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Nebula.Shared.API;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Nebula.Runtime.Misc
{
    public static class AssetManagementUtils
    {
        private static bool _didGetAssetBundlePath;
        private static string _assetBundlePath;
    
        /// <summary>
        /// Get the root patch to the local asset containers for the current environment
        /// </summary>
        public static string GetAssetsContainerPath()
        {
            if (!_didGetAssetBundlePath)
            {
                _assetBundlePath = Path.Combine(Application.isEditor 
                    ? Application.dataPath.Replace("Assets", string.Empty) 
                    : Application.persistentDataPath, "AssetContainers");
                
                _didGetAssetBundlePath = true;
            }

            return _assetBundlePath;
        }
    
        /// <summary>
        /// Init the local storage folder on where to store and save asset containers
        /// </summary>
        public static async Task InitAssetContainersDirectory()
        {
            var path = GetAssetsContainerPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                var index = new AssetsIndex();
                await StoreAssetsIndex(index);
            }
        }
        
        /// <summary>
        /// Init the local storage folder on where to store and save asset containers
        /// </summary>
        public static async Task ClearAllAssets()
        {
            var path = GetAssetsContainerPath();
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            await InitAssetContainersDirectory();
        }

        /// <summary>
        /// Load an AssetBundle from the local storage
        /// </summary>
        /// <param name="assetId">Id of the asset</param>
        public static AssetBundle LoadBundle(string assetId)
        {
            return AssetBundle.LoadFromFile(Path.Combine(GetAssetsContainerPath(), assetId));
        }
        
        /// <summary>
        /// Load an AssetBundle from the local storage async
        /// </summary>
        /// <param name="assetId">Id of the asset</param>
        public static Task<AssetBundle> LoadBundleAsync(string assetId)
        {
            var completionSource = new TaskCompletionSource<AssetBundle>();
            var request = AssetBundle.LoadFromFileAsync(Path.Combine(GetAssetsContainerPath(), assetId, assetId));
            request.completed += operation =>
            {
                completionSource.SetResult(request.assetBundle);
            };
            
            return completionSource.Task;
        }
        
        /// <summary>
        /// Locally store the AssetsIndex
        /// </summary>
        public static async Task StoreAssetsIndex(AssetsIndex index)
        {
            var path = Path.Combine(GetAssetsContainerPath(), "index.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var json = JsonConvert.SerializeObject(index, Formatting.Indented);
            await File.WriteAllTextAsync(path, json);
        }

        /// <summary>
        /// Load the local AssetsIndex
        /// </summary>
        public static async Task<AssetsIndex> LoadAssetsIndex()
        {
            var path = Path.Combine(GetAssetsContainerPath(), "index.json");
            if (!File.Exists(path))
            {
                return new AssetsIndex();
            }

            var json = await File.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<AssetsIndex>(json);
        }

        /// <summary>
        /// Download a remote asset and overwrite if locally already present 
        /// </summary>
        /// <param name="assetId">Id of the target asset</param>
        /// <param name="blobUrl">Url to the asset</param>
        /// <returns>Was download successful</returns>
        public static async Task<bool> DownloadAsset(string assetId, string blobUrl)
        {
            var pathLocalDirectory = Path.Combine(GetAssetsContainerPath(), assetId);
            if (!Directory.Exists(pathLocalDirectory))
            {
                Directory.CreateDirectory(pathLocalDirectory);
            }
            
            Debug.Log($"Downloading [{assetId}] asset container release from {blobUrl}");
            var downloadFileResponse = await DownloadFile(blobUrl);
            if (!downloadFileResponse.IsSuccess)
            {
                Debug.LogError($"Could not load root asset container release from {blobUrl}");
                return false;
            }

            var pathDownloadZip = Path.Combine(pathLocalDirectory, "download.zip");
            if (File.Exists(pathDownloadZip))
            {
                File.Delete(pathDownloadZip);
            }
            await File.WriteAllBytesAsync(pathDownloadZip, downloadFileResponse.Content);
            
            // Extract ZIP file and overwrite existing files
            ZipFile.ExtractToDirectory(pathDownloadZip, pathLocalDirectory, overwriteFiles: true);

            // Delete the ZIP file after extraction
            File.Delete(pathDownloadZip);

            return true;
        }
        
        /// <summary>
        /// Delete an asset 
        /// </summary>
        /// <param name="assetId">Id of the target asset</param>
        public static void DeleteAsset(string assetId)
        {
            var pathLocalDirectory = Path.Combine(GetAssetsContainerPath(), assetId);
            if (Directory.Exists(pathLocalDirectory))
            {
                Directory.Delete(pathLocalDirectory, true);
            }
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