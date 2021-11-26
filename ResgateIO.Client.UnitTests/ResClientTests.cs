using System;
using System.Threading.Tasks;
using Xunit;

namespace ResgateIO.Client.UnitTests
{
    public class ResClientTests
    {
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
            var client = new ResClient(() => Task.FromResult<IWebSocket>(ws));

            var connectTask = client.ConnectAsync();

            var req = await ws.GetRequestAsync();
            req.SendResult(new
            {
                protocol = "1.2.2",
            });
            await connectTask;

            Assert.Equal("1.2.2", client.ResgateProtocol);
        }
    }
}
