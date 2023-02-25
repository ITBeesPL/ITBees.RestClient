namespace ITBees.RestClient.Interfaces.RestModelMarkup
{
    /// <summary>
    /// Base class for simplify rest endpoint resolving. If Your endpoint is different than class name - You should override method GetApiEndpointUrl. Dedicated to use with other abstract classes - Vm, Im, Um, Dm
    /// </summary>
    public abstract class EndpointUrlBasedOnModelName : IEndpointUrlBasedOnModelName
    {
        public virtual string GetApiEndpointUrl()
        {
            var className = this.GetType().Name;
            if (className.EndsWith("Vm") || className.EndsWith("Im") || className.EndsWith("Um") || className.EndsWith("Dm"))
            {
                var apiEndpointUrl = className.Substring(0, className.Length - 2);
                return apiEndpointUrl;
            }

            return className;
        }
    }

}