using System.Collections.Generic;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces.RestModelMarkup;

namespace ITBees.RestClient.Interfaces
{
    public interface IRestClient<T> where T : Vm
    {
        Task<List<T>> GetMany(IClassTransformableToGetQuery objectWithQuery);
        Task<List<T>> GetMany(string endpoint, IClassTransformableToGetQuery objectWithQuery);
        Task<List<T>> GetMany(string endpoint, string queryParameters);
        Task<List<T>> GetMany(string queryUrl);

        Task<T> Get(string queryUrl);
        Task<T> Get(string endpoint, string queryParameters);
        Task<T> Get(string endpoint, IClassTransformableToGetQuery objectWithQuery);
        Task<T> Get(IClassTransformableToGetQuery objectWithQuery);

        Task<T> Post<T2>(string endpoint, T2 postModel) where T2 : Im;
        Task<T> Put<T2>(string endpoint, T2 updateModel) where T2 : Um;
        Task<T> Put<T2>(T2 updateModel) where T2 : Um;

        Task Delete(string endpoint);
        Task Delete<T2>(T2 deleteModel) where T2 : Dm;
    }
}