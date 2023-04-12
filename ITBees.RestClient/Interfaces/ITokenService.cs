using System.Threading.Tasks;

namespace ITBees.RestClient.Interfaces
{
    public interface ITokenService
    {
        string Token { get; set; }
        Task<string> DoLogin(string username, string password, string language);
        Task<bool> DoLogin();
    }
}