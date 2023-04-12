namespace ITBees.RestClient.Interfaces
{

    public class LoginInput
    {
        public string Username { get; }
        public string Password { get; }
        public string Language { get; set; }

        public LoginInput(string username, string password, string language)
        {
            Username = username;
            Password = password;
            Language = language;
        }
    }
}