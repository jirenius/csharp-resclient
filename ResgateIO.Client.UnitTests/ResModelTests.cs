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

        public static IEnumerable<object[]> ChangeEvent_UpdatesModel_Data => new List<object[]>
        {
            // Change single value
            new object[] { "{\"foo\":\"baz\"}", new { foo = "bar" }, new { foo = "baz" }, new JObject { { "foo", "baz" }, { "int", 42 } } },
            // Delete single value
            new object[] { "{\"foo\":{\"action\":\"delete\"}}", new { foo = "bar" }, new { foo = ResAction.Delete }, new JObject { { "int", 42 } } },
            // Create new value
            new object[] { "{\"new\":true}", new JObject { { "new", ResAction.Delete } }, new JObject { { "new", true } }, new JObject { { "foo", "bar" }, { "int", 42 }, { "new", true } } },
            // Change multiple values
            new object[] { "{\"foo\":\"baz\",\"int\":12}",new JObject { { "foo", "bar" }, { "int", 42 } },new JObject { { "foo", "baz" }, { "int", 12 } }, new JObject { { "foo", "baz" }, { "int", 12 } } },
        };

        [Theory, MemberData(nameof(ChangeEvent_UpdatesModel_Data))]
        public async Task ChangeEvent_UpdatesModel(string changeData, object oldValues, object newValues, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.GetAsync("test.model");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.model");
            req1.SendResult(new JObject { { "models", new JObject {
                { "test.model", new JObject {
                    { "foo", "bar" },
                    { "int", 42 },
                } }
            } } });
            var model1 = await creqTask1 as ResModel;

            var completionSource = new TaskCompletionSource<ResourceEventArgs>();
            EventHandler<ResourceEventArgs> h = (object sender, ResourceEventArgs e) => completionSource.SetResult(e);
            model1.ResourceEvent += h;

            byte[] eventMsg = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.model.change\",\"data\":{\"values\":" + changeData + "}}");
            WebSocket.SendMessage(eventMsg);

            var ev = await completionSource.Task;

            model1.ResourceEvent -= h;

            Assert.IsType<ModelChangeEventArgs>(ev);
            var changeEv = (ModelChangeEventArgs)ev;


            Test.AssertEqualJSON(oldValues, changeEv.OldValues);
            Test.AssertEqualJSON(newValues, changeEv.NewValues);
            Test.AssertEqualJSON(expected, model1);
        }

        [Fact]
        public async Task GetAsync_ChangeEventWithNoChange_TriggersNoEvent()
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.GetAsync("test.model");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.model");
            req1.SendResult(new JObject { { "models", new JObject {
                { "test.model", new JObject {
                    { "foo", "bar" },
                    { "int", 42 },
                } }
            } } });
            var model1 = await creqTask1 as ResModel;

            var completionSource = new TaskCompletionSource<ResourceEventArgs>();
            EventHandler<ResourceEventArgs> h = (object sender, ResourceEventArgs e) => completionSource.SetResult(e);
            model1.ResourceEvent += h;

            byte[] eventMsg1 = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.model.change\",\"data\":{\"values\":{\"foo\":\"bar\"}}}");
            WebSocket.SendMessage(eventMsg1);

            byte[] eventMsg2 = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.model.change\",\"data\":{\"values\":{\"int\":12}}}");
            WebSocket.SendMessage(eventMsg2);

            var ev = await completionSource.Task;

            model1.ResourceEvent -= h;

            Assert.IsType<ModelChangeEventArgs>(ev);
            var changeEv = (ModelChangeEventArgs)ev;

            Test.AssertEqualJSON( new JObject { { "int", 42 } }, changeEv.OldValues);
            Test.AssertEqualJSON(new JObject { { "int", 12 } }, changeEv.NewValues);
            Test.AssertEqualJSON(new JObject { { "foo", "bar" }, { "int", 12 } }, model1);
        }
    }
}
