using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
#pragma warning disable 0649 // These fields are assigned by JSON deserialization

    class MessageDto
    {
        [JsonProperty(PropertyName = "id")]
        public int? Id;

        [JsonProperty(PropertyName = "result")]
        public JToken Result;

        [JsonProperty(PropertyName = "error")]
        public ResError Error;

        [JsonProperty(PropertyName = "event")]
        public string Event;

        [JsonProperty(PropertyName = "data")]
        public JToken Data;
    }

#pragma warning restore 0649
}
