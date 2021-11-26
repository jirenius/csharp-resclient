using Newtonsoft.Json;

namespace ResgateIO.Client.UnitTests
{
#pragma warning disable 0649 // These fields are assigned by JSON deserialization

    class ResponseDto
    {
        [JsonProperty(PropertyName = "id")]
        public int Id;

        [JsonProperty(PropertyName = "result")]
        public object Result;
        
        [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore)]
        public object Error;
    }

#pragma warning restore 0649
}
