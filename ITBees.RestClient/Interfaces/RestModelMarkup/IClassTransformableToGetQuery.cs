namespace ITBees.RestClient.Interfaces.RestModelMarkup
{
    public interface IClassTransformableToGetQuery : IEndpointUrlBasedOnModelName
    {
        string CreateGetQueryFromClassProperties();
    }
}