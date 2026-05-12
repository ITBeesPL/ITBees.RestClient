using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InheritedMapper;
using ITBees.RestClient.Interfaces;
using ITBees.RestClient.Interfaces.RestModelMarkup;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ITBees.RestClient
{
    public class RestClient<T> : IRestClient<T> where T : Vm, new()
    {
        private readonly IHttpClient _client;
        private readonly ITokenService _tokenService;
        private readonly IWebapiEndpointSetup _webapiEndpointSetup;
        private bool _isTokenSet;

        private bool RequestInRetry;

        public TimeSpan? RequestTimeout { get; set; }

        // Number of additional attempts on top of the initial call when a request fails with
        // a timeout (OperationCanceledException) or transient network error (HttpRequestException).
        // 0 = no retry (default, backward-compatible). 1 = up to 2 attempts total. Etc.
        // 4xx/5xx HTTP status codes are NOT retried — those throw as before.
        public int MaxRetryAttempts { get; set; }

        // Delay between retry attempts. Default 0 (immediate). Keep small — RestClient is used
        // from latency-sensitive paths (e.g., gate opening on parking columns).
        public TimeSpan RetryDelay { get; set; } = TimeSpan.Zero;

        public RestClient(IWebapiEndpointSetup webapiEndpointSetup, ITokenService tokenService, IHttpClient httpClient)
        {
            _webapiEndpointSetup = webapiEndpointSetup;
            _tokenService = tokenService;
            _client = httpClient;
        }

        public RestClient(IWebapiEndpointSetup webapiEndpointSetup, ITokenService tokenService)
        {
            _webapiEndpointSetup = webapiEndpointSetup;
            _tokenService = tokenService;
            _client = new HttpClientWrapper();
        }

        public RestClient(string apiUrl)
        {
            _webapiEndpointSetup = new AnonymousWebapiEndpointSetup(apiUrl);
            _tokenService = new NoTokenNeeded();
            _client = new HttpClientWrapper();
            _isTokenSet = true;
        }

        public async Task<T> Get(string queryUrl)
        {
            await HandleTokenAuthorization();
            if (queryUrl.StartsWith("/") == false)
                queryUrl = $"/{queryUrl}";
            var requestUri = $"{_webapiEndpointSetup.WebApiUrl.Trim()}{queryUrl.Trim()}";

            var result = await ExecuteWithRetryAsync(
                ct => _client.GetAsync(requestUri, ct),
                () => _client.GetAsync(requestUri));
            if (result.IsSuccessStatusCode)
            {
                var readAsStringAsync = await result.Content.ReadAsStringAsync();
                if (result.StatusCode == HttpStatusCode.NoContent)
                {
                    return null;
                }

                return (T)new DerivedVmClassResolver<T>().Get(readAsStringAsync);
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
            return await Get($"{objectWithQuery.GetApiEndpointUrl()}?{objectWithQuery.CreateGetQueryFromClassProperties()}");
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
            await HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
            // Serialize once, recreate StringContent per attempt — HttpContent stream is one-shot
            // (after a failed send the body may be partially consumed and is not safe to resend).
            var json = JsonConvert.SerializeObject(updateModel);

            var result = await ExecuteWithRetryAsync(
                ct =>
                {
                    var content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return _client.PutAsync(requestUri, content, ct);
                },
                () =>
                {
                    var content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return _client.PutAsync(requestUri, content);
                });
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
            await HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
            var result = await ExecuteWithRetryAsync(
                ct => _client.DeleteAsync(requestUri, ct),
                () => _client.DeleteAsync(requestUri));
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

        /// <summary>
        /// Returns list of view models from endpoint. Endpoint url will be created using 's' letter for example MyAccountVm will generate 'https://localhost:0000/myAccounts' endpoint
        /// </summary>
        /// <param name="objectWithQuery"></param>
        /// <returns></returns>
        public async Task<List<T>> GetMany(IClassTransformableToGetQuery objectWithQuery)
        {
            return await GetMany(
                $"{objectWithQuery.GetApiEndpointUrl()}s/{objectWithQuery.CreateGetQueryFromClassProperties()}");
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
            await HandleTokenAuthorization();
            if (queryUrl.StartsWith("/") == false)
                queryUrl = $"/{queryUrl}";
            var requestUri = $"{_webapiEndpointSetup.WebApiUrl.Trim()}{queryUrl.Trim()}";

            var result = await ExecuteWithRetryAsync(
                ct => _client.GetAsync(requestUri, ct),
                () => _client.GetAsync(requestUri));
            if (result.IsSuccessStatusCode)
            {
                var readAsStringAsync = await result.Content.ReadAsStringAsync();

                return (List<T>)new DerivedVmClassResolver<T>().GetMany(readAsStringAsync);
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
            await HandleTokenAuthorization();
            var requestUri = GetRequestUri(endpoint);
            var json = JsonConvert.SerializeObject(model);

            var result = await ExecuteWithRetryAsync(
                ct =>
                {
                    var content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return _client.PostAsync(requestUri, content, ct);
                },
                () =>
                {
                    var content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return _client.PostAsync(requestUri, content);
                });
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

        // Wraps a single HTTP request operation with per-attempt timeout (RequestTimeout) and
        // retry on timeout / transient network failure (MaxRetryAttempts). HTTP-level errors
        // (4xx/5xx) bubble up to the caller without retry — those are decision points for the
        // caller, not transient transport issues.
        //
        // Two factories so backward compatibility is preserved: when no timeout/retry is
        // configured we call the original non-CT IHttpClient overloads (existing custom impls
        // / Moq setups don't need to change). When timeout or retry is opt-in we switch to
        // the CT overloads (default-implemented on IHttpClient, inherited "for free" by
        // HttpClientWrapper from System.Net.Http.HttpClient).
        //
        // Both factories are invoked fresh each attempt — callers must reconstruct any
        // single-use HttpContent inside them (see Put/Post).
        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            Func<CancellationToken, Task<HttpResponseMessage>> operationWithCt,
            Func<Task<HttpResponseMessage>> operationLegacy)
        {
            if (!RequestTimeout.HasValue && MaxRetryAttempts <= 0)
            {
                // Opt-out path: prior behavior, no CT, no retry. Preserves compatibility with
                // every IHttpClient impl that exists today (none override the CT overloads).
                return await operationLegacy().ConfigureAwait(false);
            }

            var totalAttempts = 1 + System.Math.Max(0, MaxRetryAttempts);
            Exception? lastTransientException = null;

            for (int attempt = 1; attempt <= totalAttempts; attempt++)
            {
                CancellationTokenSource? cts = null;
                try
                {
                    if (RequestTimeout.HasValue)
                    {
                        cts = new CancellationTokenSource(RequestTimeout.Value);
                        return await operationWithCt(cts.Token).ConfigureAwait(false);
                    }

                    return await operationWithCt(CancellationToken.None).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    // Per-attempt timeout fired. Capture and retry if budget remains.
                    lastTransientException = new TimeoutException(
                        $"Request timed out after {RequestTimeout?.TotalMilliseconds:F0} ms on attempt {attempt}/{totalAttempts}.",
                        ex);
                }
                catch (HttpRequestException ex)
                {
                    // Connection refused / DNS / TLS / unexpected EOF — treat as transient.
                    lastTransientException = ex;
                }
                finally
                {
                    cts?.Dispose();
                }

                if (attempt < totalAttempts && RetryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(RetryDelay).ConfigureAwait(false);
                }
            }

            // Exhausted all attempts — rethrow the last transient failure so the caller can
            // distinguish it (TimeoutException / HttpRequestException) from other exceptions.
            throw lastTransientException!;
        }

        private async Task HandleTokenAuthorization()
        {
            if (string.IsNullOrEmpty(_tokenService.Token))
            {
                await _tokenService.DoLogin();
            }

            if (_isTokenSet == false)
            {
                if (_client.DefaultRequestHeaders != null)
                {
                    _client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _tokenService.Token);
                }
                _isTokenSet = true;
            }
        }
    }
}
