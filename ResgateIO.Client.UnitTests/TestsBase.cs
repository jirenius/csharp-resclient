using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    /// <summary>
    /// Initializes a mock connection, a test logger, and a service with the name "test".
    /// </summary>
    public abstract class TestsBase : IDisposable
    {
        public readonly ITestOutputHelper Output;
        public readonly ResClient Client;
        public MockWebSocket WebSocket { get; private set; }
        public MockResgate Resgate { get; private set; }

        public TestsBase(ITestOutputHelper output)
        {
            Output = output;
            Client = new ResClient(createWebSocket);
        }

        public async Task ConnectAndHandshake(string protocol = "1.2.2")
        { 
            var connectTask = Client.ConnectAsync();
            await Resgate.HandshakeAsync(protocol);
            await connectTask;

            Assert.Equal(protocol, Client.ResgateProtocol);
        }

        private Task<IWebSocket> createWebSocket()
        {
            WebSocket = new MockWebSocket();
            Resgate = new MockResgate(WebSocket);
            return Task.FromResult<IWebSocket>(WebSocket);
        }

        public void Dispose()
        {
            Client.Dispose();
            WebSocket?.Dispose();
        }
    }
}
