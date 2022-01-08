using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ResClientTests : TestsBase
    {
        public ResClientTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ResClient_CreatesInstance()
        {
            var client = new ResClient("ws://127.0.0.1:8080");
            Assert.NotNull(client);
        }

        [Fact(Skip = "requires resgate listening on port 8080")]
        public async Task ConnectAsync_ConnectsToResgate()
        {
            var client = new ResClient("ws://127.0.0.1:8080");
            await client.ConnectAsync();

            Assert.Equal("1.2.2", client.ResgateProtocol);
        }

        [Fact]
        public async Task ConnectAsync_ConnectsToMockWebSocket()
        {
            var ws = new MockWebSocket();
            var resgate = new MockResgate(ws);
            var client = new ResClient(() => Task.FromResult<IWebSocket>(ws));
            
            var connectTask = client.ConnectAsync();
            await resgate.HandshakeAsync("1.2.2");
            await connectTask;

            Assert.Equal("1.2.2", client.ResgateProtocol);
        }

        [Fact]
        public async Task GetAsync_WithModelResponse_GetsModel()
        {
            await ConnectAndHandshake();

            var creqTask = Client.GetAsync("test.model");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.model");
            req.SendResult(new JObject
            {
                { "models", new JObject
                    {
                        { "test.model", new JObject
                            {
                                { "foo", "bar" }
                            }
                        }
                    }
                }
            });
            var result = await creqTask;
            Assert.Equal("test.model", result.ResourceID);
            Assert.IsType<ResModel>(result);

            var model = result as ResModel;
            Assert.Contains("foo", model.Props.Keys);
            Assert.Equal("bar", model.Props["foo"]);
        }
    }
}
