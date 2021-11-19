using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ResgateIO.Client
{
    public class ResClient
    {
        // Constants
        /// <value>Supported RES protocol version.</value>
        public readonly string ProtocolVersion = "1.2.1";

        // Properties

        /// <value>Supported RES protocol version by Resgate.</value>
        public string ResgateProtocol { get; private set; }

        // Fields
        private ResRpc rpc;
        private string hostUrl;
        private readonly Func<Task<IWebSocket>> wsFactory;
        private JsonSerializerSettings serializerSettings;
        private int protocol;

        // Private constants
        private const string legacyProtocol = "1.1.1";

        public ResClient(string hostUrl)
        {
            this.hostUrl = hostUrl;
            this.wsFactory = createWebSocket;
        }

        public ResClient(Func<Task<IWebSocket>> wsFactory)
        {
            this.wsFactory = wsFactory;
        }

        /// <summary>
        /// Sets the settings used with JSON serialization.
        /// Must be called before connecting.
        /// </summary>
        /// <param name="settings">JSON serializer settings.</param>
        /// <returns>The ResClient instance.</returns>
        public ResClient SetSerializerSettings(JsonSerializerSettings settings)
        {
            serializerSettings = settings;
            return this;
        }

        public async Task ConnectAsync()
        {
            if (rpc != null)
            {
                return;
            }
            var ws = await wsFactory();

            rpc = new ResRpc(ws, serializerSettings);

            protocol = 0;
            // RES protocol version handshake
            try
            {
                var result = await rpc.Request("version", new VersionRequestDto(ProtocolVersion));
                if (result.Result != null)
                {
                    var versionResponse = result.Result.ToObject<VersionResponseDto>();
                    protocol = versionToInt(versionResponse.Protocol);
                    ResgateProtocol = versionResponse.Protocol;
                }
            }
            catch (ResException ex)
            {
                // An invalid request error means legacy behavior
                if (ex.Code != ResError.CodeInvalidRequest)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // Set legacy protocol.
            if (protocol == 0)
            {
                protocol = versionToInt(legacyProtocol);
                ResgateProtocol = legacyProtocol;
            }

            // this.ws.onopen = this._handleOnopen;
            // this.ws.onerror = this._handleOnerror;
            // this.ws.onmessage = this._handleOnmessage;
            // this.ws.onclose = this._handleOnclose;
        }

        private void onMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine(e.Message.ToString());
        }

        private async Task<IWebSocket> createWebSocket()
        {
            var webSocket = new WebSocket();
            await webSocket.ConnectAsync(hostUrl);
            return webSocket;
        }

        private static int versionToInt(string version)
        {
            if (String.IsNullOrEmpty(version))
            {
                return 0;
            }

            var v = 0;
            try {
                var parts = version.Split('.');
                foreach (var part in parts)
                {
                    v = v * 1000 + Int32.Parse(part);
                }
            } catch (Exception)
            {
                return 0;
            }
            return v;
        }
    }
}
