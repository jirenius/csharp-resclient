using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResgateIO.Client.UnitTests
{
    /// <summary>
    /// MockResgate contains methods to simplify mocking Resgate behavior on a MockWebSocket.
    /// </summary>
    public class MockResgate
    {
        public readonly MockWebSocket WebSocket;
        public string Protocol { get; private set; }

        public MockResgate(MockWebSocket webSocket)
        {
            WebSocket = webSocket;
        }

        /// <summary>
        /// HandshakeAsync awaits the version request and reponds to it.
        /// </summary>
        /// <param name="protocol">Optional protocol version.</param>
        /// <returns></returns>
        public async Task HandshakeAsync(string protocol = "1.2.2")
        {
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("version").AssertParams(new { protocol = ResClient.ProtocolVersion });
            req.SendResult(new
            {
                protocol = protocol,
            });
            Protocol = protocol;
        }
    }
}
