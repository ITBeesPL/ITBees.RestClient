using System.Collections.Generic;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces.RestModelMarkup;

namespace ITBees.RestClient.Interfaces
{
    public interface IRestClient<TViewModel> where TViewModel : Vm, new()
    {
        Task<List<TViewModel>> GetMany(IClassTransformableToGetQuery objectWithQuery);
        Task<List<TViewModel>> GetMany(string endpoint, IClassTransformableToGetQuery objectWithQuery);
        Task<List<TViewModel>> GetMany(string endpoint, string queryParameters);
        Task<List<TViewModel>> GetMany(string queryUrl);

        Task<TViewModel> Get(string queryUrl);
        Task<TViewModel> Get(string endpoint, string queryParameters);
        Task<TViewModel> Get(string endpoint, IClassTransformableToGetQuery objectWithQuery);
        Task<TViewModel> Get(IClassTransformableToGetQuery objectWithQuery);

        Task<TViewModel> Post(string endpoint, IInputOrViewModel postModel);
        Task<TViewModel> Post(IInputOrViewModel postModel);

        Task<TViewModel> Put(string endpoint, IUm updateModel);
        Task<TViewModel> Put(IUm updateModel);

        Task Delete(string endpoint);
        Task Delete(IDm deleteModel);
    }
}