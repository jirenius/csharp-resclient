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

        [Theory]
        [InlineData("\"foo\"", "foo")]
        [InlineData("null", null)]
        [InlineData("42", 42)]
        [InlineData("true", true)]
        [InlineData("[\"foo\",null,42,true]", new object[] { "foo", null, 42, true })]
        public async Task AuthAsync_AnonymousResult_GetsResult(string payload, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.GetAsync("test.model");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.model");
            req1.SendResult(new JObject { { "models", new JObject {
                { "test.model", Test.Model }
            } } });
            var model1 = await creqTask1 as ResModel;

            var creqTask2 = model1.AuthAsync("method");
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("auth.test.model.method");
            req2.SendResult(new JObject { { "payload", JToken.Parse(payload) } });
            var result = await creqTask2;
            Test.AssertEqualJSON(expected == null ? JValue.CreateNull() : JToken.FromObject(expected), result);
        }

        [Fact]
        public async Task GetAsync_WithCustomModelFactory_GetsCustomModel()
        {
            Client.RegisterModelFactory("test.custom.*", (client, rid) => new Test.CustomModel(client, rid));
            await ConnectAndHandshake();

            var creqTask = Client.GetAsync("test.custom.42");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.custom.42");
            req.SendResult(new JObject
            {
                { "models", new JObject
                    {
                        { "test.custom.42", Test.CustomModelData }
                    }
                }
            });
            var result = await creqTask;
            Assert.Equal("test.custom.42", result.ResourceID);
            Assert.IsType<Test.CustomModel>(result);
            var model = result as Test.CustomModel;
            Assert.Equal("foo", model.String);
            Assert.Equal(42, model.Int);
        }
    }
}
