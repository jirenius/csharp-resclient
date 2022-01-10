using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Xunit;

namespace ResgateIO.Client.UnitTests
{
    public static class Test
    {
        public static readonly JObject Model = new JObject { { "foo", "bar" } };
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
