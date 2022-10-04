using Newtonsoft.Json;

namespace ResgateIO.Client
{
#pragma warning disable 0649 // These fields are assigned by JSON deserialization

    class VersionResponseDto
    {
        [JsonProperty(PropertyName = "protocol")]
        public string Protocol;
    }

#pragma warning restore 0649
}
