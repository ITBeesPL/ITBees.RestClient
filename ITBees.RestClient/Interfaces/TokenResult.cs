using System;

namespace ITBees.RestClient.Interfaces
{
    public class TokenResult
    {
        public string Value { get; set; }
        public DateTime TokenExpirationDate { get; set; }
    }
}