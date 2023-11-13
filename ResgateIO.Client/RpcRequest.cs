using Newtonsoft.Json;

namespace ResgateIO.Client
{
    internal class RpcRequest
    {
        [JsonProperty(PropertyName = "id")]
        public int Id;
        [JsonProperty(PropertyName = "method")]
        public string Method;
        [JsonProperty(PropertyName = "params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params;
        [JsonIgnore]
        public ResponseCallback Callback;
    }
}