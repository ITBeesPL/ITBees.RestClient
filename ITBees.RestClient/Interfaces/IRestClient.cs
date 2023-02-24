using System.Collections.Generic;
using System.Threading.Tasks;

namespace ITBees.RestClient.Interfaces
{
    public interface IRestClient<T>
    {
        Task<List<T>> GetMany(string queryUrl);
        Task<T> Get(string queryUrl);
        Task<T> Post<T2>(string url, T2 model) where T2 :class;
        Task<T> Put(string url, T model);
        Task Delete(string url);
    }
}