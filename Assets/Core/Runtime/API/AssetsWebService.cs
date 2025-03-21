using System.Collections.Generic;
using System.Threading.Tasks;
using Nebula.Runtime.API.Dtos;
using Nebula.Shared.API;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Nebula.Runtime.API
{
    public class AssetsWebservice
    {
        public bool IsAuthenticated { get; private set; }
        
        private readonly string _endpoint;
        private string _authToken;
        
        private const string PREFSKEY_AUTH_TOKEN = "Nebula_AuthToken";

        public AssetsWebservice(string baseUrl)
        {
            _endpoint = baseUrl;
            if (PlayerPrefs.HasKey(PREFSKEY_AUTH_TOKEN))
            {
                _authToken = PlayerPrefs.GetString(PREFSKEY_AUTH_TOKEN);
                IsAuthenticated = !string.IsNullOrEmpty(_authToken);
            }
            else
            {
                IsAuthenticated = false;
            }
        }

        public void OverrideAuthToken(string authToken)
        {
            _authToken = authToken;
        }
        
        public Task<WebResponse<TokenDto>> Login(LoginDto dto)
        {
            var completionSource = new TaskCompletionSource<WebResponse<TokenDto>>();
            
            var form = new WWWForm();
            form.AddField(nameof(dto.Email), dto.Email);
            form.AddField(nameof(dto.Password), dto.Password);
            
            var request = UnityWebRequest.Post($"{_endpoint}/auth/login", form);
            request.SendWebRequest().completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completionSource.SetResult(WebResponse<TokenDto>.Failed(request.error));
                }
                else
                {
                    var responseDto = JsonConvert.DeserializeObject<TokenDto>(request.downloadHandler.text);
                    PlayerPrefs.SetString(PREFSKEY_AUTH_TOKEN, responseDto.Token);
                    _authToken = responseDto.Token;
                    IsAuthenticated = true;
                    
                    completionSource.SetResult(WebResponse<TokenDto>.Success(responseDto));
                }
            };
            
            return completionSource.Task;
        }

        public void Logout()
        {
            PlayerPrefs.DeleteKey(PREFSKEY_AUTH_TOKEN);
            _authToken = string.Empty;
            IsAuthenticated = false;
        }
        
        public Task<WebResponse<List<AssetDto>>> GetAllAssets()
        {
            var completionSource = new TaskCompletionSource<WebResponse<List<AssetDto>>>();
            var request = UnityWebRequest.Get($"{_endpoint}/assets");
            request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
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
            request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
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
