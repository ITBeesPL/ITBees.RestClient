namespace ITBees.RestClient.Interfaces
{

    public class LoginInput
    {
        public string Username { get; }
        public string Password { get; }

        public LoginInput(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}