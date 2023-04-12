namespace ITBees.RestClient.Interfaces
{
    public interface IWebapiEndpointSetup
    {
        string Login { get; set; }
        string Pass { get; set; }
        string WebApiUrl { get; set; }
        string LoginEndpoint { get; set; }
        public string Language { get; set; }
        void ReloadSettings();
    }
}