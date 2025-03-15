using System.Collections.Generic;
using System.Threading.Tasks;
using Nebula.Runtime.API.Dtos;
using Nebula.Shared.API;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Nebula.Runtime.API
{
    public class AssetsWebservice
    {
        private readonly string _endpoint;

        public AssetsWebservice(string baseUrl)
        {
            _endpoint = baseUrl + "/manage";
        }
        
        public Task<WebResponse<List<AssetDto>>> GetAllAssets()
        {
            var completionSource = new TaskCompletionSource<WebResponse<List<AssetDto>>>();
            var request = UnityWebRequest.Get($"{_endpoint}/assets");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<List<AssetDto>>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<List<AssetDto>>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<List<AssetDto>>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetDto>> GetAsset(string assetId)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetDto>>();
            var request = UnityWebRequest.Get($"{_endpoint}/assets/{assetId}");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
    }
}
