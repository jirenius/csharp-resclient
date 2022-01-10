using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace ResgateIO.Client.UnitTests
{
    public static class Test
    {
        public static readonly JObject Model = new JObject { { "foo", "bar" } };

        public static void AssertEqualJSON(object expected, object actual)
        {
            JToken expectedToken = expected as JToken ?? JToken.FromObject(expected);
            JToken actualToken = actual as JToken ?? JToken.FromObject(actual);
            Assert.True(JToken.DeepEquals(expectedToken, actualToken), String.Format("Expected JSON: {0}\nActual JSON:   {1}", expectedToken.ToString(), actualToken.ToString()));
        }
    }
}
