﻿using System;
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
            get
            {
                if (_token == null)
                {
                    throw new Exception("Not logged in.");
                }
                return _token;
            }
            set => _token = value;
        }

        public async Task<string> DoLogin(IWebapiEndpointSetup webapiEndpointSetup)
        {
            return await DoLogin(webapiEndpointSetup.Login, webapiEndpointSetup.Pass);
        }

        public async Task<bool> DoLogin()
        {
            if (_apiEndpointSetup == null)
                throw new Exception("Token service must be initialized with ApiEndpointSetup service");

            Token = await DoLogin(_apiEndpointSetup.Login, _apiEndpointSetup.Pass);
            return string.IsNullOrEmpty(Token) == false;
        }

        public async Task<string> DoLogin(string username, string password)
        {
            var loginPair = new LoginInput(username, password);

            var stringContent = new StringContent(JsonSerializer.Serialize(loginPair), System.Text.Encoding.UTF8, "application/json");
            var requestUri = new Uri($"{_apiEndpointSetup.WebApiUrl}/{_apiEndpointSetup.LoginEndpoint}");
            var result = await _client.PostAsync(requestUri, stringContent);
            if (result.IsSuccessStatusCode)
            {
                Token = await result.Content.ReadAsStringAsync();
                return Token;
            }

            throw new Exception(result.ReasonPhrase);
        }
    }
}