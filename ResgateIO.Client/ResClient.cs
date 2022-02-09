using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ResgateIO.Client
{

    public class ResourceEventArgs : EventArgs
    {
        public string ResourceID { get; set; }
        public string EventName { get; set; }
    }

    public class ResClient : IDisposable
    {
        // Constants
        /// <value>Supported RES protocol version.</value>
        public const string ProtocolVersion = "1.2.1";

        public static object DeleteValue = new object();

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
        private Dictionary<string, CacheItem> itemCache = new Dictionary<string, CacheItem>();
        //private HashSet<string> stale = new HashSet<string>();
        private object cacheLock = new object();
        private IResourceType[] resourceTypes;

        // Private constants
        private const string legacyProtocol = "1.1.1";

        public ResClient(string hostUrl)
        {
            this.hostUrl = hostUrl;
            this.wsFactory = createWebSocket;
            createResourceTypes();
        }

        public ResClient(Func<Task<IWebSocket>> wsFactory)
        {
            this.wsFactory = wsFactory;
            createResourceTypes();
        }

        private void createResourceTypes()
        {
             resourceTypes = new IResourceType[]
             {
                 new ResourceTypeModel(this),
                 new ResourceTypeCollection(this)
             };
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
        /// The pattern may contain wildcards.
        /// </summary>
        /// <param name="pattern">Resource name pattern.</param>
        /// <param name="factory">Model factory delegate.</param>
        public void RegisterModelFactory(string pattern, ModelFactory factory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Registers a collection factory for a specific resource pattern.
        /// The pattern may contain wildcards.
        /// </summary>
        /// <param name="pattern">Resource name pattern.</param>
        /// <param name="factory">Collection factory delegate.</param>
        //public void RegisterCollectionFactory(string pattern, CollectionFactory factory)
        //{
        //    throw new NotImplementedException();
        //}

        public void RegisterCollectionFactory(string pattern, CollectionFactory factory)
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// Get a resource from the API.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <returns>The resource.</returns>
        public async Task<ResResource> GetAsync(string rid)
        {
            CacheItem ci;
            lock (cacheLock)
            {
                if (!itemCache.TryGetValue(rid, out ci))
                {
                    ci = new CacheItem(this, rid);
                    itemCache[rid] = ci;
                    Task _ = subscribeAsync(ci);
                }
            }

            return await ci.ResourceTask;
        }

        /// <summary>
        /// Calls an API resource method.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The resource.</returns>
        public async Task<object> CallAsync(string rid, string method, object parameters)
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calls an API resource method and returns the result as a value of type T.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The resource.</returns>
        public async Task<T> CallAsync<T>(string rid, string method, object parameters)
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        private async Task subscribeAsync(CacheItem ci)
        {
            var rid = ci.ResourceID;
            ci.AddSubscription(1);

            RequestResult result;
            try
            {
                result = await sendAsync("subscribe", rid, null, null);
            }
            catch (Exception ex)
            {
                ci.AddSubscription(-1);
                tryDelete(ci);
                ci.TrySetException(ex);
                throw ex;
            }

            lock (cacheLock)
            {
                cacheResources(result);
            }
        }

        private void cacheResources(RequestResult result)
        {
            if (result.Result == null)
            {
                return;
            }

            JObject r = result.Result as JObject;
            if (r == null)
            {
                return;
            }

            JObject[] typeResources = new JObject[resourceTypes.Length];
            Dictionary<string, JToken>[] typeSync = new Dictionary<string, JToken>[resourceTypes.Length];

            // Create empty resources for missing ones, and a dictionary of already existing resources to be synchronized.
            for (int i = 0; i < resourceTypes.Length; i++) {
                IResourceType type = resourceTypes[i];
                JProperty resourceProp = r.Property(type.ResourceProperty);
                if (resourceProp != null)
                {
                    JObject resources = resourceProp.Value as JObject;
                    if (resources != null)
                    {
                        typeResources[i] = resources;
                        typeSync[i] = createResources(resources, type);
                    }
                }
            }

            // Initialize new resources with data
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                IResourceType type = resourceTypes[i];
                JObject resources = typeResources[i];
                if (resources != null)
                {
                    var sync = typeSync[i];
                    foreach (JProperty prop in resources.Properties())
                    {
                        string rid = prop.Name;
                        // Only initialize if not set for synchronization
                        if (sync == null || !sync.ContainsKey(rid))
                        {
                            type.InitResource(itemCache[rid].Resource, prop.Value);
                        }
                    }
                }
            }

            // Synchronize stale resources with new data
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                var sync = typeSync[i];
                if (sync != null)
                {
                    IResourceType type = resourceTypes[i];
                    foreach (KeyValuePair<string, JToken> pair in sync)
                    {
                        type.SynchronizeResource(itemCache[pair.Key].Resource, pair.Value);
                    }
                }
            }

            // Complete all resource tasks
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                IResourceType type = resourceTypes[i];
                JObject resources = typeResources[i];
                if (resources != null)
                {
                    foreach (JProperty prop in resources.Properties())
                    {
                        string rid = prop.Name;
                        itemCache[rid].CompleteTask();
                    }
                }
            }
        }

        private Dictionary<string, JToken> createResources(JObject resources, IResourceType type)
        {
            Dictionary<string, JToken> sync = null;

            foreach (JProperty prop in resources.Properties())
            {
                string rid = prop.Name;
                CacheItem ci = null;
                if (!itemCache.TryGetValue(rid, out ci))
                {
                    // If the resource is not cached since before, create a new cache item for it.
                    ci = new CacheItem(this, rid);
                    itemCache[rid] = ci;
                }
                else
                {
                    // If the resource was cached, it might have been stale.
                    // removeStale(rid)
                }

                // If it is set since before, it is stale and needs to be updated
                if (ci.IsSet)
                {
                    if (ci.Resource.Type != type.ResourceType)
                    {
                        throw new InvalidOperationException("Resource type inconsistency");
                    }

                    sync = sync ?? new Dictionary<string, JToken>();
                    sync[rid] = prop.Value;
                }
                else
                {
                    ci.SetResource(type.CreateResource(rid));
                }
            }

            return sync;
        }

        private void tryDelete(CacheItem ci)
        {
            throw new NotImplementedException();
        }

        //private void removeStale(string rid)
        //{
        //    stale.Remove(rid);
        //}

        internal object ParseValue(JToken value, bool addIndirect)
        {
            if (value.Type == JTokenType.Null)
            {
                return null;
            }

            var obj = value as JObject;
            if (obj != null)
            {
                // Test for resource reference
                JToken ridToken = obj["rid"];
                if (ridToken != null)
                {
                    var rid = ridToken.Value<string>();

                    // Test for soft reference
                    JToken softToken = obj["soft"];
                    if (softToken != null && softToken.Value<bool>())
                    {
                        return new ResRef(rid);
                    }

                    CacheItem item = itemCache[rid];
                    if (addIndirect)
                    {
                        item.AddReference(1);
                    }
                    return item.Resource;
                }

                // Test for data value
                JToken dataToken = obj["data"];
                if (dataToken != null)
                {
                    return dataToken;
                }

                // Test for action value
                JToken actionToken = obj["actiona"];
                if (actionToken != null)
                {
                    if (actionToken.Value<string>() == "delete")
                    {
                        return ResClient.DeleteValue;
                    }
                }
            }
            else
            {
                var val = value as JValue;
                if (val != null)
                {
                    return val.Value;
                }
            }

            throw new InvalidOperationException("Invalid RES value: " + value.ToString(Formatting.None));
        }

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
