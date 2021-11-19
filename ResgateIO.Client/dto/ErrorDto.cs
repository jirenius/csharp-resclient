using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
#pragma warning disable 0649 // These fields are assigned by JSON deserialization

    class ErrorDto
    {
        [JsonProperty(PropertyName = "code")]
        public string Code;

        [JsonProperty(PropertyName = "message")]
        public string Message;

        [JsonProperty(PropertyName = "data")]
        public JToken Data;
    }

#pragma warning restore 0649
}
