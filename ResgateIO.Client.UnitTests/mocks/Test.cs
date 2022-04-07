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

            public override void HandleEvent(ResourceEventArgs ev)
            {
                switch (ev)
                {
                    case ModelChangeEventArgs changeEv:
                        if (changeEv.NewValues.TryGetValue("string", out object stringValue))
                        {
                            String = stringValue as string;
                        }
                        if (changeEv.NewValues.TryGetValue("int", out object intValue))
                        {
                            Int = Convert.ToInt32(intValue);
                        }
                        break;
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
            { "model.a", new JObject { { "foo", "bar" } } },
            { "model.b-a", new JObject { { "a", new JObject { { "rid", "model.a" } } } } },
            { "model.c-ab", new JObject { { "a", new JObject { { "rid", "model.a" } } }, { "b", new JObject { { "rid", "model.b-a" } } } } },
            { "model.d-e", new JObject { { "e", new JObject { { "rid", "model.e-d" } } } } },
            { "model.e-d", new JObject { { "d", new JObject { { "rid", "model.d-e" } } } } },
            { "model.f-bd", new JObject { { "b", new JObject { { "rid", "model.b-a" } } }, { "d", new JObject { { "rid", "model.d-e" } } } } },

            // Collection
            { "test.collection", new JArray { "foo", "bar" } },
            { "collection.g-a", new JArray { new JObject { { "rid", "model.a" } } } }


        };

        public static void AssertEqualJSON(object expected, object actual)
        {
            JToken expectedToken = expected as JToken ?? JToken.FromObject(expected);
            JToken actualToken = actual as JToken ?? JToken.FromObject(actual);
            Assert.True(JToken.DeepEquals(expectedToken, actualToken), String.Format("Expected JSON: {0}\nActual JSON:   {1}", expectedToken.ToString(), actualToken.ToString()));
        }

        public static JObject ResourceSet(params string[] rids)
        {
            var models = new JObject();
            var collections = new JObject();

            foreach (string rid in rids)
            {
                var resource = Resources[rid];
                if (resource is JObject)
                {
                    models[rid] = resource;
                } else if (resource is JArray)
                {
                    collections[rid] = resource;
                } else
                {
                    throw new Exception("Resource of unknown type");
                }                
            }

            return new JObject { { "models", models }, { "collections", collections } };
        }
    }
}
