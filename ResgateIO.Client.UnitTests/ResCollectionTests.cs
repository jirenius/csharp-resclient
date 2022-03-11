using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ResCollectionTests : TestsBase
    {
        public ResCollectionTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("\"foo\"", "foo")]
        [InlineData("null", null)]
        [InlineData("42", 42)]
        [InlineData("true", true)]
        [InlineData("[\"foo\",null,42,true]", new object[] { "foo", null, 42, true })]
        public async Task CallAsync_AnonymousResult_GetsResult(string payload, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.GetAsync("test.collection");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.collection");
            req1.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", Test.Collection }
            } } });
            var collection1 = await creqTask1 as ResCollection;

            var creqTask2 = collection1.CallAsync("method");
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("call.test.collection.method");
            req2.SendResult(new JObject { { "payload", JToken.Parse(payload) } });
            var result = await creqTask2;
            Test.AssertEqualJSON(expected == null ? JValue.CreateNull() : JToken.FromObject(expected), result);
        }

        [Theory]
        [InlineData("\"foo\"", "foo")]
        [InlineData("null", null)]
        [InlineData("42", 42)]
        [InlineData("true", true)]
        [InlineData("[\"foo\",null,42,true]", new object[] { "foo", null, 42, true })]
        public async Task AuthAsync_AnonymousResult_GetsResult(string payload, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.GetAsync("test.collection");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.collection");
            req1.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", Test.Collection }
            } } });
            var collection1 = await creqTask1 as ResCollection;

            var creqTask2 = collection1.AuthAsync("method");
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("auth.test.collection.method");
            req2.SendResult(new JObject { { "payload", JToken.Parse(payload) } });
            var result = await creqTask2;
            Test.AssertEqualJSON(expected == null ? JValue.CreateNull() : JToken.FromObject(expected), result);
        }
    }
}
