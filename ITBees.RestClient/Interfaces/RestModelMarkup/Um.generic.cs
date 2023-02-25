namespace ITBees.RestClient.Interfaces.RestModelMarkup
{
    /// <summary>
    /// Rest api Update model base class, Use it with ITBees.RestClient to simplify rest endpoint resolving. If Your endpoint is different than class name - You should override method GetApiEndpointUrl
    /// </summary>
    public abstract class Um : EndpointUrlBasedOnModelName
    {

    }
}