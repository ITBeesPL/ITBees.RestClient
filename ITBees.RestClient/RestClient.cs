using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces;

namespace ITBees.RestClient
{
    public class RestClient<T> : IRestClient<T> where T : class
    {
        private readonly IWebapiEndpointSetup _webapiEndpointSetup;
        private readonly ITokenService _tokenService;
        private readonly HttpClient _client;
        private bool _isTokenSet;

        public RestClient(IWebapiEndpointSetup webapiEndpointSetup, ITokenService tokenService)
        {
            _webapiEndpointSetup = webapiEndpointSetup;
            _tokenService = tokenService;
            _client = new HttpClient();
        }

        public async Task<T> Get(string queryUrl)
        {
            HandleTokenAuthorization();
            var result = await HttpResponseMessage(queryUrl);
            if (result.IsSuccessStatusCode)
            {
                var readAsStringAsync = await result.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(readAsStringAsync, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
                {
                    RequestInRetry = true;
                    return await Get(queryUrl);
                }
                else
                {
                    RequestInRetry = false;
                }

            }

            throw new Exception(result.ReasonPhrase);
        }


        private bool RequestInRetry = false;

        public async Task<T> Post<T2>(string url, T2 model) where T2 : class
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(url);
            var result = await _client.PostAsJsonAsync(requestUri, model);
            if (result.IsSuccessStatusCode)
            {
                var readAsStringAsync = await result.Content.ReadAsStringAsync();
                var deserialized = JsonSerializer.Deserialize<T>(readAsStringAsync, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
                return deserialized;
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
                {
                    RequestInRetry = true;
                    return await Post(url, model);
                }
                else
                {
                    RequestInRetry = false;
                }
            }

            throw new Exception(result.ReasonPhrase);
        }

        private string GetRequestUri(string url)
        {
            if (url.StartsWith("/") == false)
                url = $"/{url}";
            var requestUri = $"{_webapiEndpointSetup.WebApiUrl}{url}";
            return requestUri;
        }

        public async Task<T> Put(string url, T model)
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(url);
            var result = await _client.PutAsJsonAsync(requestUri, model);
            if (result.IsSuccessStatusCode)
            {
                var readAsStringAsync = await result.Content.ReadAsStringAsync();
                var deserialized = JsonSerializer.Deserialize<T>(readAsStringAsync, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
                return deserialized;
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
                {
                    RequestInRetry = true;
                    return await Post<T>(url, model);
                }
                else
                {
                    RequestInRetry = false;
                }

            }

            throw new Exception(result.ReasonPhrase);
        }

        private async Task<bool> RefreshToken()
        {
            _isTokenSet = false;
            _tokenService.Token = string.Empty;
            return await _tokenService.DoLogin();
        }

        public async Task Delete(string url)
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(url);
            var result = await _client.DeleteAsync(requestUri);
            if (result.IsSuccessStatusCode)
            {
                return;
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
                {
                    RequestInRetry = true;
                    await Delete(url);
                }
                else
                {
                    RequestInRetry = false;
                }

            }
            throw new Exception(result.ReasonPhrase);
        }

        public async Task<List<T>> GetMany(string queryUrl)
        {
            HandleTokenAuthorization();

            var result = await HttpResponseMessage(queryUrl);
            if (result.IsSuccessStatusCode)
            {
                var readAsStringAsync = await result.Content.ReadAsStringAsync();
                var deserialized = JsonSerializer.Deserialize<List<T>>(readAsStringAsync, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
                return deserialized;
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
                {
                    RequestInRetry = true;
                    return await GetMany(queryUrl);
                }
                else
                {
                    RequestInRetry = false;
                }

            }

            throw new Exception(result.ReasonPhrase);
        }

        private async Task<HttpResponseMessage> HttpResponseMessage(string queryUrl)
        {
            if (queryUrl.StartsWith("/") == false)
                queryUrl = $"/{queryUrl}";
            var requestUri = $"{_webapiEndpointSetup.WebApiUrl}{queryUrl}";
            var result = await _client.GetAsync(requestUri);
            return result;
        }

        private void HandleTokenAuthorization()
        {
            if (_isTokenSet == false)
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenService.Token);
                _isTokenSet = true;
            }
        }
    }
}