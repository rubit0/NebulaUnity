using System.Collections.Generic;
using System.Threading.Tasks;
using Nebula.Editor.API.Dtos.Requests;
using Nebula.Editor.API.Dtos.Responses;
using Nebula.Shared.API;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Nebula.Editor.API
{
    public class ManagementWebService
    {
        private readonly string _endpoint;

        public ManagementWebService(string baseUrl)
        {
            _endpoint = baseUrl + "/manage";
        }
        
        public Task<WebResponse<List<AssetContainerDto>>> GetAllContainer()
        {
            var completionSource = new TaskCompletionSource<WebResponse<List<AssetContainerDto>>>();
            var request = UnityWebRequest.Get($"{_endpoint}/container");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<List<AssetContainerDto>>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<List<AssetContainerDto>>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<List<AssetContainerDto>>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetContainerDto>> GetContainer(string containerId)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetContainerDto>>();
            var request = UnityWebRequest.Get($"{_endpoint}/container/{containerId}");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetContainerDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetContainerDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetContainerDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<AssetContainerDto>> CreateNewContainer(CreateContainerDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<AssetContainerDto>>();
            var form = new WWWForm();
            form.AddField(nameof(dto.Name), dto.Name);
            
            var request = UnityWebRequest.Post($"{_endpoint}/container", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<AssetContainerDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<AssetContainerDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<AssetContainerDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<ReleaseDto>> GetRelease(string containerId, string releaseId)
        {
            var completionSource = new TaskCompletionSource<WebResponse<ReleaseDto>>();
            var request = UnityWebRequest.Get($"{_endpoint}/container/{containerId}/releases/{releaseId}");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<ReleaseDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<ReleaseDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<ReleaseDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<List<ReleaseDto>>> GetReleases(string containerId)
        {
            var completionSource = new TaskCompletionSource<WebResponse<List<ReleaseDto>>>();
            var request = UnityWebRequest.Get($"{_endpoint}/container/{containerId}/releases");
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<List<ReleaseDto>>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<List<ReleaseDto>>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<List<ReleaseDto>>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<ReleaseDto>> AddRelease(string containerId, UploadReleaseDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<ReleaseDto>>();
            var form = new WWWForm();
            form.AddField(nameof(dto.Notes), dto.Notes);
            
            var request = UnityWebRequest.Post($"{_endpoint}/container/{containerId}/releases", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<ReleaseDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<ReleaseDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<ReleaseDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse<ReleaseDto>> AppendPackage(string containerId, string releaseId, UploadPackageDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<ReleaseDto>>();
            var form = new WWWForm();
            form.AddBinaryData(nameof(dto.FileMain), dto.FileMain);
            form.AddField(nameof(dto.PackagePlatform), dto.PackagePlatform);
            
            var request = UnityWebRequest.Post($"{_endpoint}/container/{containerId}/releases/{releaseId}/packages", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<ReleaseDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<ReleaseDto>(request.downloadHandler.text);
                    completionSource.SetResult(WebResponse<ReleaseDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse> UpdateReleaseChannelSlot(string containerId, ReleaseChannel channel, string releaseId)
        {
            var completionSource = new TaskCompletionSource<WebResponse>();
            var form = new WWWForm();
            var request = UnityWebRequest.Post($"{_endpoint}/container/{containerId}/slot/{channel}/{releaseId}", form);
            request.SendWebRequest().completed += operation =>
            {
                completionSource.SetResult(request.result != UnityWebRequest.Result.Success
                    ? WebResponse.Failed(request.error)
                    : WebResponse.Success());
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse> UpdateContainerMeta(string containerId, UpdateContainerMetaDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse>();
            var form = new WWWForm();
            foreach (var entry in dto.Meta)
            {
                form.AddField($"{nameof(UpdateContainerMetaDto.Meta)}[{entry.Key}]", entry.Value);
            }
            
            var request = UnityWebRequest.Post($"{_endpoint}/container/{containerId}/meta", form);
            request.SendWebRequest().completed += operation =>
            {
                completionSource.SetResult(request.result != UnityWebRequest.Result.Success
                    ? WebResponse.Failed(request.error)
                    : WebResponse.Success());
            };
            
            return completionSource.Task;
        }
        
        public Task<WebResponse> UpdateContainerAccessGroups(string containerId, UpdateContainerAccessGroupsDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse>();
            var form = new WWWForm();
            form.AddField(nameof(dto.AccessGroups), JsonConvert.SerializeObject(dto.AccessGroups));
            var request = UnityWebRequest.Post($"{_endpoint}/container/{containerId}/access", form);
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
