using System.Threading.Tasks;
using ITBees.RestClient.Interfaces;

namespace ITBees.RestClient;

public class NoTokenNeeded : ITokenService
{
    public string Token { get; set; } = "token not needed";
    public async Task<string> DoLogin(string username, string password, string language)
    {
        return string.Empty;
    }

    public async Task<bool> DoLogin()
    {
        return true;
    }
}