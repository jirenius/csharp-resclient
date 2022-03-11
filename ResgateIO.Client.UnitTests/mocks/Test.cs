using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ResgateIO.Client.UnitTests
{
    public static class Test
    {
        /// <summary>
        /// Mail represents a mail message resource model.
        /// </summary>
        public class CustomModel : ResModelResource
        {
            public string String { get; private set; }
            public int Int { get; private set; }

            public readonly ResClient Client;

            public CustomModel(ResClient client, string rid) : base(rid)
            {
                Client = client;
            }

            public override void Init(IReadOnlyDictionary<string, object> props)
            {
                String = props["string"] as string;
                Int = Convert.ToInt32(props["int"]);
            }

            public override void HandleChange(IReadOnlyDictionary<string, object> props)
            {
                if (props.TryGetValue("string", out object stringValue))
                {
                    String = stringValue as string;
                }
                if (props.TryGetValue("int", out object intValue))
                {
                    Int = Convert.ToInt32(intValue);
                }
            }
        }

        public class Payload
        {
            [JsonProperty(PropertyName = "foo")]
            public string Foo;
        }

        public static readonly JObject Model = new JObject { { "foo", "bar" } };
        public static readonly JObject CustomModelData = new JObject { { "string", "foo" }, { "int", 42 } };
        public static readonly JArray Collection = new JArray { "foo", "bar" };

        public static readonly Dictionary<string, JToken> Resources = new Dictionary<string, JToken>
        {
            // Models
            { "test.model", new JObject { { "foo", "bar" } } },
            { "test.model.parent", new JObject { { "ref", new JObject { { "rid", "test.model" } } } } },

            // Collection
            { "test.collection", new JArray { "foo", "bar" } },
        };

        public static void AssertEqualJSON(object expected, object actual)
        {
            JToken expectedToken = expected as JToken ?? JToken.FromObject(expected);
            JToken actualToken = actual as JToken ?? JToken.FromObject(actual);
            Assert.True(JToken.DeepEquals(expectedToken, actualToken), String.Format("Expected JSON: {0}\nActual JSON:   {1}", expectedToken.ToString(), actualToken.ToString()));
        }
    }
}
