using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces;
using ITBees.RestClient.Interfaces.RestModelMarkup;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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

        public async Task<T> Get(string endpoint, string queryParameters)
        {
            return await Get($"{endpoint}?{queryParameters}");
        }

        public async Task<T> Get(string endpoint, IClassTransformableToGetQuery objectWithQuery)
        {
            return await Get($"{endpoint}?{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        public async Task<T> Get(IClassTransformableToGetQuery objectWithQuery)
        {
            return await Get($"{objectWithQuery.GetApiEndpointUrl()}/{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        async Task<T> IRestClient<T>.Post(string endpoint, IInputOrViewModel postModel)
        {
            return await Post(endpoint, postModel);
        }

        public async Task<T> Post(IInputOrViewModel postModel)
        {
            return await Post(postModel.GetApiEndpointUrl(), postModel);
        }

        public async Task<T> Put(string endpoint, IUm updateModel)
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
            var content = new StringContent(JsonConvert.SerializeObject(updateModel));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var result = await _client.PutAsync(requestUri, content);
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
                return await Put(endpoint, updateModel);
            }

            RequestInRetry = false;

            throw new Exception(result.ReasonPhrase);
        }

        public async Task<T> Put(IUm updateModel) 
        {
            return await Put(updateModel.GetApiEndpointUrl(), updateModel);
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

        public async Task Delete(IDm deleteModel)
        {
            await Delete($"{deleteModel.GetApiEndpointUrl()}?{deleteModel.CreateGetQueryFromClassProperties()}");
        }

        public async Task<List<T>> GetMany(IClassTransformableToGetQuery objectWithQuery)
        {
            return await GetMany(
                $"{objectWithQuery.GetApiEndpointUrl()}/{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        public async Task<List<T>> GetMany(string endpoint, IClassTransformableToGetQuery objectWithQuery)
        {
            return await GetMany($"{endpoint}/{objectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        public async Task<List<T>> GetMany(string endpoint, string queryParameters)
        {
            return await GetMany($"{endpoint}?{queryParameters}");
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

        public async Task<T> Post(string endpoint, IIm model)
        {
            HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
            var content = new StringContent(JsonConvert.SerializeObject(model));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var result = await _client.PostAsync(requestUri, content);
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

        private async Task<bool> RefreshToken()
        {
            _isTokenSet = false;
            _tokenService.Token = string.Empty;
            return await _tokenService.DoLogin();
        }

        public async Task<List<T>> GetMany(string endpoint,
            ClassTransformableToGetQuery classTransformableToGetObjectWithQuery)
        {
            return await GetMany($"{endpoint}/{classTransformableToGetObjectWithQuery.CreateGetQueryFromClassProperties()}");
        }

        private async Task<HttpResponseMessage> HttpResponseMessage(string queryUrl)
        {
            if (queryUrl.StartsWith("/") == false)
                queryUrl = $"/{queryUrl}";
            var requestUri = $"{_webapiEndpointSetup.WebApiUrl.Trim()}{queryUrl.Trim()}";
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