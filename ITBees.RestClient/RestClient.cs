using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces;
using ITBees.RestClient.Interfaces.RestModelMarkup;

namespace ITBees.RestClient
{
    public class RestClient<T> : IRestClient<T> where T : Vm
    {
        private readonly HttpClient _client;
        private readonly ITokenService _tokenService;
        private readonly IWebapiEndpointSetup _webapiEndpointSetup;
        private bool _isTokenSet;

        private bool RequestInRetry;

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

            if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
            {
                RequestInRetry = true;
                return await Get(queryUrl);
            }

            RequestInRetry = false;

            throw new Exception(result.ReasonPhrase);
        }

        public Task<T> Get(string endpoint, string queryParameters)
        {
            return Get($"{endpoint}?{queryParameters}");
        }

        public Task<T> Get(string endpoint, IClassTransformableToGetQuery objectWithQuery)
        {
            return Get($"{endpoint}?{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        public Task<T> Get(IClassTransformableToGetQuery objectWithQuery)
        {
            return Get($"{objectWithQuery.GetApiEndpointUrl()}/{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        Task<T> IRestClient<T>.Post<T2>(string endpoint, T2 postModel)
        {
            return Post(endpoint, postModel);
        }

        public async Task<T> Put<T2>(string endpoint, T2 updateModel) where T2 : Um
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
            var result = await _client.PutAsJsonAsync(requestUri, updateModel);
            if (result.IsSuccessStatusCode)
            {
                var readAsStringAsync = await result.Content.ReadAsStringAsync();
                var deserialized = JsonSerializer.Deserialize<T>(readAsStringAsync, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
                return deserialized;
            }

            if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
            {
                RequestInRetry = true;
                return await Post(endpoint, updateModel);
            }

            RequestInRetry = false;

            throw new Exception(result.ReasonPhrase);
        }

        public Task<T> Put<T2>(T2 updateModel) where T2 : Um
        {
            return Put(updateModel.GetApiEndpointUrl(), updateModel);
        }

        public async Task Delete(string endpoint)
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
            var result = await _client.DeleteAsync(requestUri);
            if (result.IsSuccessStatusCode) return;

            if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
            {
                RequestInRetry = true;
                await Delete(endpoint);
            }
            else
            {
                RequestInRetry = false;
            }

            throw new Exception(result.ReasonPhrase);
        }

        public Task Delete<T2>(T2 deleteModel) where T2 : Dm
        {
            return Delete($"{deleteModel.GetApiEndpointUrl()}?{deleteModel.CreateGetQueryFromClassProperties()}");
        }

        public Task<List<T>> GetMany(IClassTransformableToGetQuery objectWithQuery)
        {
            return GetMany(
                $"{objectWithQuery.GetApiEndpointUrl()}/{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        public Task<List<T>> GetMany(string endpoint, IClassTransformableToGetQuery objectWithQuery)
        {
            return GetMany($"{endpoint}/{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        public Task<List<T>> GetMany(string endpoint, string queryParameters)
        {
            return GetMany($"{endpoint}?{queryParameters}");
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

            if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
            {
                RequestInRetry = true;
                return await GetMany(queryUrl);
            }

            RequestInRetry = false;

            throw new Exception(result.ReasonPhrase);
        }

        public async Task<T> Post<T2>(string endpoint, T2 model) where T2 : class
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
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

            if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
            {
                RequestInRetry = true;
                return await Post(endpoint, model);
            }

            RequestInRetry = false;

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

            if (result.StatusCode == HttpStatusCode.Unauthorized && RequestInRetry == false && await RefreshToken())
            {
                RequestInRetry = true;
                return await Post(url, model);
            }

            RequestInRetry = false;

            throw new Exception(result.ReasonPhrase);
        }


        private async Task<bool> RefreshToken()
        {
            _isTokenSet = false;
            _tokenService.Token = string.Empty;
            return await _tokenService.DoLogin();
        }

        public Task<List<T>> GetMany(string endpoint,
            ClassTransformableToGetQuery classTransformableToGetObjectWithQuery)
        {
            return GetMany($"{endpoint}/{classTransformableToGetObjectWithQuery.CreateGetQueryFromClassProperties()}");
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
                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokenService.Token);
                _isTokenSet = true;
            }
        }
    }
}