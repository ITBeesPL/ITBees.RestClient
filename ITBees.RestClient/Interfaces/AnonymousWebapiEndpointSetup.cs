namespace ITBees.RestClient.Interfaces;

public class AnonymousWebapiEndpointSetup : IWebapiEndpointSetup
{
    public AnonymousWebapiEndpointSetup(string webApiUrl)
    {
        WebApiUrl = webApiUrl;
    }
    public string Login { get; set; }
    public string Pass { get; set; }
    public string WebApiUrl { get; set; }
    public string LoginEndpoint { get; set; }
    public string MyAccountEndpoint { get; set; }
    public string Language { get; set; }
    public void ReloadSettings()
    {
            
    }
}