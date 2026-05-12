using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ITBees.RestClient
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent content);
        Task<HttpResponseMessage> GetAsync(string requestUri);
        HttpRequestHeaders DefaultRequestHeaders { get; }
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
        Task<HttpResponseMessage> DeleteAsync(string requestUri);
        Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent content, CancellationToken cancellationToken)
            => PutAsync(requestUri, content);

        Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
            => GetAsync(requestUri);

        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
            => PostAsync(requestUri, content);

        Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken)
            => DeleteAsync(requestUri);
    }
}