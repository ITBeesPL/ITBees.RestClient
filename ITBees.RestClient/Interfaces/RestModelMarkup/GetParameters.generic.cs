using System.Reflection;
using System.Text;

namespace ITBees.RestClient.Interfaces.RestModelMarkup
{
    public abstract class ClassTransformableToGetQuery : EndpointUrlBasedOnModelName, IClassTransformableToGetQuery
    {
        public string CreateGetQueryFromClassProperties()
        {
            PropertyInfo[] properties = this.GetType().GetProperties();
            StringBuilder sb = new StringBuilder();
            var firstRound = true;
            foreach (var property in properties)
            {
                if (firstRound == false)
                {
                    sb.Append("&");
                }
                sb.Append(property.Name).Append("=").Append(property.GetValue(this)).Append("");
                firstRound = false;
            }
            return sb.ToString();
        }
    }
}