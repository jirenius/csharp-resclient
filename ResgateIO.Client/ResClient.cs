using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ResgateIO.Client
{

    public class ResClient : IDisposable
    {
        // Constants
        /// <value>Supported RES protocol version.</value>
        public const string ProtocolVersion = "1.2.1";

        // Events
        public event EventHandler<ResourceEventArgs> ResourceEvent;

        // Properties

        /// <value>Supported RES protocol version by Resgate.</value>
        public string ResgateProtocol { get; private set; }

        // Fields
        private ResRpc rpc;
        private string hostUrl;
        private readonly Func<Task<IWebSocket>> wsFactory;
        private JsonSerializerSettings serializerSettings;
        private int protocol;
        private bool disposedValue;
        //private Dictionary<string, CacheItem> itemCache = new Dictionary<string, CacheItem>();

        private ItemCache cache;
        //private HashSet<string> stale = new HashSet<string>();
        //private object cacheLock = new object();
        //private IResourceType[] resourceTypes;

        // Private constants
        private const string legacyProtocol = "1.1.1";
        private static int legacyProtocolVersion = versionToInt(legacyProtocol);

        private const int resourceTypeModel = 0;
        private const int resourceTypeCollection = 1;

        public ResClient(string hostUrl)
        {
            this.hostUrl = hostUrl;
            this.wsFactory = createWebSocket;
            createItemCache();
        }

        public ResClient(Func<Task<IWebSocket>> wsFactory)
        {
            this.wsFactory = wsFactory;
            createItemCache();
        }

        private void createItemCache()
        {
            cache = new ItemCache(this);
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

        /// <summary>
        /// Registers a model factory for a specific resource pattern.
        /// The pattern may contain wildcards:
        /// * (asterisk) is a partial wildcard.
        ///     Eg. "directory.user.*.details"
        /// > (greater than) is a full wildcard.
        ///     Eg. "library.books.>"
        /// </summary>
        /// <param name="pattern">Resource name pattern.</param>
        /// <param name="factory">Model factory delegate.</param>
        public void RegisterModelFactory(string pattern, ModelFactory factory)
        {
            cache.RegisterModelFactory(pattern, factory);
        }

        /// <summary>
        /// Registers a collection factory for a specific resource pattern.
        /// The pattern may contain wildcards:
        /// * (asterisk) is a partial wildcard.
        ///     Eg. "directory.user.*.details"
        /// > (greater than) is a full wildcard.
        ///     Eg. "library.books.>"
        /// </summary>
        /// <param name="pattern">Resource name pattern.</param>
        /// <param name="factory">Collection factory delegate.</param>
        public void RegisterCollectionFactory(string pattern, CollectionFactory factory)
        {
            cache.RegisterCollectionFactory(pattern, factory);
        }

        public async Task ConnectAsync()
        {
            if (rpc != null)
            {
                return;
            }
            var ws = await wsFactory();

            rpc = new ResRpc(ws, serializerSettings);
            rpc.ResourceEvent += onResourceEvent;


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

        /// <summary>
        /// Get a resource from the API.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <returns>The resource.</returns>
        public Task<ResResource> GetAsync(string rid)
        {
            CacheItem ci = cache.GetOrSubscribe(rid, subscribe);

            return ci.ResourceTask;
        }

        /// <summary>
        /// Sends a request to an API resource call method.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<object> CallAsync(string rid, string method, object parameters)
        {
            return requestAsync("call", rid, method, parameters);
        }

        /// <summary>
        /// Sends a request to an API resource call method.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<object> CallAsync(string rid, string method)
        {
            return CallAsync(rid, method, null);
        }

        /// <summary>
        /// Sends a request to an API resource call method and returns the result as a value of type T.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<T> CallAsync<T>(string rid, string method, object parameters)
        {
            return requestAsync<T>("call", rid, method, parameters);
        }

        /// <summary>
        /// Sends a request to an API resource call method and returns the result as a value of type T.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<T> CallAsync<T>(string rid, string method)
        {
            return CallAsync<T>(rid, method, null);
        }

        /// <summary>
        /// Sends a request to an API resource authentication method.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<object> AuthAsync(string rid, string method, object parameters)
        {
            return requestAsync("auth", rid, method, parameters);
        }

        /// <summary>
        /// Sends a request to an API resource authentication method.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<object> AuthAsync(string rid, string method)
        {
            return AuthAsync(rid, method, null);
        }

        /// <summary>
        /// Sends a request to an API resource authentication method and returns the result as a value of type T.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<T> AuthAsync<T>(string rid, string method, object parameters)
        {
            return requestAsync<T>("auth", rid, method, parameters);
        }

        /// <summary>
        /// Sends a request to an API resource authentication method and returns the result as a value of type T.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<T> AuthAsync<T>(string rid, string method)
        {
            return AuthAsync<T>(rid, method, null);
        }

        // _call
        private async Task<object> requestAsync(string type, string rid, string method, object parameters)
        {
            RequestResult result = await sendAsync(type, rid, method, parameters);

            if (protocol <= legacyProtocolVersion)
            {
                return result.Result;
            }

            if (result.Result == null)
            {
                return null;
            }

            JObject r = result.Result as JObject;
            if (r == null)
            {
                return null;
            }

            // Check if the result is a resource response
            if (r.ContainsKey("rid"))
            {
                var resourceID = (string)r["rid"];

                CacheItem ci = cache.AddResourcesAndSubscribe(result.Result, resourceID);
                return ci.Resource;
            }

            return r["payload"];
        }


        // _call
        private async Task<T> requestAsync<T>(string type, string rid, string method, object parameters)
        {
            var o = await requestAsync(type, rid, method, parameters);
            if (o is T)
            {
                return (T)o;
            }

            if (o is JToken)
            {
                var token = (JToken)o;
                return token.ToObject<T>();
            }

            // Try to type cast it as a last measure
            return (T)Convert.ChangeType(o, typeof(T));
        }

        // _subscribe
        private async void subscribe(CacheItem ci)
        {
            ci.AddSubscription(1);

            RequestResult result;
            try
            {
                result = await sendAsync("subscribe", ci.ResourceID, null, null);
            }
            catch (Exception ex)
            {
                ci.AddSubscription(-1);
                cache.TryDelete(ci);
                ci.TrySetException(ex);
                throw ex;
            }

            cache.AddResources(result.Result);
        }

        // _send
        private async Task<RequestResult> sendAsync(string action, string rid, string method, object parameters)
        {
            string m = String.IsNullOrEmpty(method)
                ? action + "." + rid
                : action + "." + rid + "." + method;

            await ConnectAsync();
            return await rpc.Request(m, parameters);
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

        private void onResourceEvent(object sender, ResourceEventArgs ev)
        {
            ev = cache.HandleEvent(ev);
            if (ev != null)
            {
                ResourceEvent?.Invoke(this, ev);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    rpc?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
