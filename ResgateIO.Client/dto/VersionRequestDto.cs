using Newtonsoft.Json;

namespace ResgateIO.Client
{
    class VersionRequestDto
    {
        [JsonProperty(PropertyName = "protocol")]
        public string Protocol;

        public VersionRequestDto(string protocol)
        {
            Protocol = protocol;
        }
    }
}
