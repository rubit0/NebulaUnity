using System.Collections.Generic;
using System.Threading.Tasks;
using Nebula.Runtime.API.Dtos.Requests;
using Nebula.Runtime.API.Dtos.Responses;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Nebula.Runtime.API
{
    public class AssetsWebService
    {
        private readonly string _endpoint;

        public AssetsWebService(string baseUrl)
        {
            _endpoint = baseUrl + "/manage";
        }
        
        public Task<WebResponse<List<AssetBucketSimpleDto>>> GetAllBuckets()
        {
            var completionSource = new TaskCompletionSource<WebResponse<List<AssetBucketSimpleDto>>>();
            var request = UnityWebRequest.Get(_endpoint + "/buckets");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<List<AssetBucketSimpleDto>>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<List<AssetBucketSimpleDto>>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<List<AssetBucketSimpleDto>>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetBucketDto>> GetBucket(string bucketId)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetBucketDto>>();
            var request = UnityWebRequest.Get(_endpoint + $"/buckets/{bucketId}");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetBucketDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetBucketDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetBucketDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetBucketDto>> CreateNewAssetBucket(CreateBucketDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetBucketDto>>();
            var form = new WWWForm();
            form.AddField(nameof(dto.Name), dto.Name);
            form.AddField(nameof(dto.CRC), dto.CRC);
            form.AddBinaryData(nameof(dto.FileRoot), dto.FileRoot);
            form.AddBinaryData(nameof(dto.FileRootManifest), dto.FileRootManifest);
            
            var request = UnityWebRequest.Post(_endpoint + "/buckets", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetBucketDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetBucketDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetBucketDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetBucketSimpleDto>> UpdateRootAssetBundle(string bucketId, UpdateRootAssetBundleDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetBucketSimpleDto>>();
            var form = new WWWForm();
            form.AddField(nameof(dto.CRC), dto.CRC);
            form.AddBinaryData(nameof(dto.FileMain), dto.FileMain);
            form.AddBinaryData(nameof(dto.FileManifest), dto.FileManifest);

            var request = UnityWebRequest.Post(_endpoint + $"/buckets/{bucketId}/bundles/root", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetBucketSimpleDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetBucketSimpleDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetBucketSimpleDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetBundleDto>> UploadNewAssetBundle(string bucketId, UploadAssetBundleDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetBundleDto>>();
            var form = new WWWForm();
            form.AddField(nameof(dto.BundleName), dto.BundleName);
            form.AddField(nameof(dto.DisplayName), dto.DisplayName);
            form.AddField(nameof(dto.AssetType), dto.AssetType);
            form.AddField(nameof(dto.MetaData), JsonConvert.SerializeObject(dto.MetaData, Formatting.Indented));
            form.AddField(nameof(dto.Dependencies), JsonConvert.SerializeObject(dto.Dependencies, Formatting.Indented));
            form.AddField(nameof(dto.CRC), dto.CRC);
            form.AddBinaryData(nameof(dto.FileMain), dto.FileMain);
            form.AddBinaryData(nameof(dto.FileManifest), dto.FileManifest);

            var request = UnityWebRequest.Post(_endpoint + $"/buckets/{bucketId}/bundles", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetBundleDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetBundleDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetBundleDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetBundleDto>> UpdateAssetBundle(string bucketId, string bundleId, UploadAssetBundleDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetBundleDto>>();
            var form = new WWWForm();
            form.AddField(nameof(dto.BundleName), dto.BundleName);
            form.AddField(nameof(dto.DisplayName), dto.DisplayName);
            form.AddField(nameof(dto.AssetType), dto.AssetType);
            form.AddField(nameof(dto.MetaData), JsonConvert.SerializeObject(dto.MetaData, Formatting.Indented));
            form.AddField(nameof(dto.Dependencies), JsonConvert.SerializeObject(dto.Dependencies, Formatting.Indented));
            form.AddField(nameof(dto.CRC), dto.CRC);
            form.AddBinaryData(nameof(dto.FileMain), dto.FileMain);
            form.AddBinaryData(nameof(dto.FileManifest), dto.FileManifest);
            
            var request = UnityWebRequest.Post(_endpoint + $"/buckets/{bucketId}/bundles/{bundleId}", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetBundleDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetBundleDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetBundleDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse> DeleteAssetBundle(string bucketId, string bundleId, DeleteAssetBundleDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse>();
            var form = new WWWForm();
            form.AddField(nameof(dto.CRCRoot), dto.CRCRoot);
            form.AddBinaryData(nameof(dto.FileRoot), dto.FileRoot);
            form.AddBinaryData(nameof(dto.FileRootManifest), dto.FileRootManifest);
            
            var request = UnityWebRequest.Post(_endpoint + $"/buckets/{bucketId}/bundles/{bundleId}", form);
            request.method = "DELETE";
            request.SendWebRequest().completed += operation =>
            {
                completionSource.SetResult(request.result != UnityWebRequest.Result.Success
                    ? WebResponse.Failed(request.error)
                    : WebResponse.Success());
            };
            
            return completionSource.Task;
        }
    }
}
