using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces;

namespace ITBees.RestClient
{
    public class TokenService : ITokenService
    {
        private readonly IWebapiEndpointSetup _apiEndpointSetup;
        private readonly HttpClient _client;
        private string _token;

        public TokenService(IWebapiEndpointSetup apiEndpointSetup)
        {
            _apiEndpointSetup = apiEndpointSetup;
            _client = new HttpClient();
        }

        public string Token
        {
            get => _token;
            set => _token = value;
        }

        public async Task<string> DoLogin(IWebapiEndpointSetup webapiEndpointSetup)
        {
            return await DoLogin(webapiEndpointSetup.Login, webapiEndpointSetup.Pass, webapiEndpointSetup.Language);
        }

        public async Task<bool> DoLogin()
        {
            if (_apiEndpointSetup == null)
                throw new Exception("Token service must be initialized with ApiEndpointSetup service");

            Token = await DoLogin(_apiEndpointSetup.Login, _apiEndpointSetup.Pass, _apiEndpointSetup.Language);
            return string.IsNullOrEmpty(Token) == false;
        }

        public async Task<string> DoLogin(string username, string password, string language)
        {
            var loginPair = new LoginInput(username, password, language);

            var stringContent = new StringContent(JsonSerializer.Serialize(loginPair), System.Text.Encoding.UTF8, "application/json");
            var uriString = $"{_apiEndpointSetup.WebApiUrl.Trim()}/{_apiEndpointSetup.LoginEndpoint.Trim()}";
            var requestUri = new Uri(uriString);
            var result = await _client.PostAsync(requestUri, stringContent);
            var responseBody = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                if (responseBody.Contains("tokenExpirationDate"))
                {
                    var token = JsonSerializer.Deserialize<TokenResult>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (token != null)
                    {
                        return token.Value;
                    }
                    else
                    {
                        throw new Exception("Token result in unknown format, reponse body : " + responseBody);
                    }
                    
                }
                else
                {
                    Token = responseBody;
                    return Token;
                }
                
            }

            throw new Exception(result.ReasonPhrase + responseBody);
        }
    }
}