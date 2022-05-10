using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ResgateIO.Client
{

    internal class RpcRequest
    {
        [JsonProperty(PropertyName = "id")]
        public int Id;
        [JsonProperty(PropertyName = "method")]
        public string Method;
        [JsonProperty(PropertyName = "params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params;
        [JsonIgnore]
        public TaskCompletionSource<RequestResult> Task;
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
            this.ws.OnClose += onClose;
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

        private void onClose(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void handleResponse(MessageDto rpcmsg)
        {
            try
            {
                RpcRequest req = consumeRequest(rpcmsg.Id ?? default);

                if (rpcmsg.Error != null)
                {
                    req.Task.SetException(new ResException(rpcmsg.Error));
                }
                else
                {
                    req.Task.SetResult(new RequestResult
                    {
                        Result = rpcmsg.Result
                    });
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

        public async Task<RequestResult> Request(string method, object parameters)
        {
            var task = new TaskCompletionSource<RequestResult>();
            RpcRequest req;
            lock (requestLock)
            {
                req = new RpcRequest
                {
                    Id = this.requestId++,
                    Method = method,
                    Params = parameters,
                    Task = task
                };

                this.requests.Add(req.Id, req);
            }

            var dta = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req, serializerSettings));

            try
            {
                await this.ws.SendAsync(dta);
            }
            catch (Exception e)
            {
                consumeRequest(req.Id);
                throw e;
            }

            return await task.Task;
        }

        private RpcRequest consumeRequest(int id)
        {
            lock (requestLock)
            {
                if (this.requests.TryGetValue(id, out RpcRequest req))
                {
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
