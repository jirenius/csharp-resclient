using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResgateIO.Client
{

    internal delegate void ResponseCallback(RequestResult result, ResError err);

    internal class RpcRequest
    {
        [JsonProperty(PropertyName = "id")]
        public int Id;
        [JsonProperty(PropertyName = "method")]
        public string Method;
        [JsonProperty(PropertyName = "params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params;
        [JsonIgnore]
        public ResponseCallback Callback;
    }

    class RequestResult
    {
        public JToken Result;
    }

    class ResRpc: IDisposable
    {
        public IWebSocket WebSocket { get { return ws; } }

        // Events
        public event EventHandler<ResourceEventArgs> ResourceEvent;
        public event ErrorEventHandler Error;

        private readonly IWebSocket ws;
        private readonly JsonSerializerSettings serializerSettings;
        private int requestId = 1;

        private Dictionary<int, RpcRequest> requests = new Dictionary<int, RpcRequest>();
        private object requestLock = new object();
        private bool disposedValue;

        public ResRpc(IWebSocket ws, JsonSerializerSettings serializerSettings)
        {
            this.ws = ws;
            this.serializerSettings = serializerSettings;
            this.ws.OnMessage += onMessage;
        }

        private void onMessage(object sender, MessageEventArgs e)
        {
            string msg = null;
            MessageDto rpcmsg = null;

            try
            {
                msg = Encoding.UTF8.GetString(e.Message);
                rpcmsg = JsonConvert.DeserializeObject<MessageDto>(msg);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(new InvalidMessageException(e.Message, "Error deserializing incoming message.", ex)));
            }

            if (rpcmsg.Id != null)
            {
                handleResponse(rpcmsg);
            }
            else if (rpcmsg.Event != null)
            {
                handleEvent(rpcmsg);
            }
            else
            {
                throw new InvalidOperationException(String.Format("Invalid message from server: {0}", msg));
            }
        }

        private void handleResponse(MessageDto rpcmsg)
        {
            try
            {
                RpcRequest req = consumeRequest(rpcmsg.Id ?? default);

                if (rpcmsg.Error != null)
                {
                    req.Callback(null, rpcmsg.Error);
                }
                else
                {
                    req.Callback(new RequestResult
                    {
                        Result = rpcmsg.Result
                    }, null);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
                return;
            }
        }

        private void handleEvent(MessageDto rpcmsg)
        {
            try
            {
                // Event
                var idx = rpcmsg.Event.LastIndexOf('.');
                if (idx <  0 || idx == rpcmsg.Event.Length - 1) {
                    throw new InvalidOperationException(String.Format("Malformed event name: {0}", rpcmsg.Event));
                }
                var rid = rpcmsg.Event.Substring(0, idx);


                ResourceEvent?.Invoke(this, new ResourceEventArgs
                {
                    ResourceID = rid,
                    EventName = rpcmsg.Event.Substring(idx + 1),
                    Data = rpcmsg.Data,
                });
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
                return;
            }
        }

        public void Request(string method, object parameters, ResponseCallback callback)
        {
            RpcRequest req;
            lock (requestLock)
            {
                if (this.requests == null)
                {
                    Task.Run(() => callback(null, new ResError(ResError.CodeConnectionError, "Connection closed")));
                    return;
                }

                req = new RpcRequest
                {
                    Id = this.requestId++,
                    Method = method,
                    Params = parameters,
                    Callback = callback,
                };
                this.requests.Add(req.Id, req);
            }

            var dta = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req, serializerSettings));

            Task.Run(async () =>
            {
                try
                {
                    await this.ws.SendAsync(dta);
                }
                catch (ResException e)
                {
                    consumeRequest(req.Id);
                    callback(null, e.Error);
                }
                catch (Exception e)
                {
                    consumeRequest(req.Id);
                    callback(null, new ResError(e.Message));
                }
            });
        }

        private RpcRequest consumeRequest(int id)
        {
            lock (requestLock)
            {
                if (this.requests == null)
                {
                    throw new InvalidOperationException(String.Format("Incoming request disposed: {0}", id));
                }

                if (this.requests.TryGetValue(id, out RpcRequest req))
                {
                    this.requests.Remove(id);
                    return req;
                }
            }

            throw new InvalidOperationException(String.Format("Invalid incoming request ID: {0}", id));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Call all pending requests with a connection closed error.
                    Dictionary<int, RpcRequest> pendingRequests;
                    lock (requestLock)
                    {
                        pendingRequests = this.requests;
                        this.requests = null;
                    }
                    foreach (var req in pendingRequests)
                    {
                        Task.Run(() => req.Value.Callback(null, new ResError(ResError.CodeConnectionError, "Connection closed")));
                    }

                    ws.Dispose();
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
