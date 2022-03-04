using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ResModelTests : TestsBase
    {
        public ResModelTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("\"foo\"", "foo")]
        [InlineData("null", null)]
        [InlineData("42", 42)]
        [InlineData("true", true)]
        [InlineData("[\"foo\",null,42,true]", new object[] { "foo", null, 42, true })]
        public async Task CallAsync_WithoutParameters_CallsMethod(string payload, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.GetAsync("test.model");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.model");
            req1.SendResult(new JObject { { "models", new JObject {
                { "test.model", Test.Model }
            } } });
            var model1 = await creqTask1 as ResModel;

            var creqTask2 = model1.CallAsync("method");
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("call.test.model.method");
            req2.SendResult(new JObject { { "payload", JToken.Parse(payload) } });
            var result = await creqTask2;
            Test.AssertEqualJSON(expected == null ? JValue.CreateNull() : JToken.FromObject(expected), result);
        }
    }
}