using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ResgateIO.Client.UnitTests
{
#pragma warning disable 0649 // These fields are assigned by JSON deserialization

    class RequestDto
    {
        [JsonProperty(PropertyName = "id")]
        public int Id;

        [JsonProperty(PropertyName = "method")]
        public string Method;
        
        [JsonProperty(PropertyName = "params", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Params;
    }

#pragma warning restore 0649
}
