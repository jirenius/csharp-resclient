using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    internal delegate void ResponseCallback(RequestResult result, ResError err);

    internal class ResRpc : IDisposable
    {
        // Events
        public event EventHandler<ResourceEventArgs> ResourceEvent;
        public event ErrorEventHandler Error;

        private readonly object _requestLock = new object();

        private readonly JsonSerializerSettings _serializerSettings;

        private int _requestId = 1;
        private Dictionary<int, RpcRequest> _requests = new Dictionary<int, RpcRequest>();
        private bool _disposedValue;

        public IWebSocket WebSocket { get; }

        public ResRpc(IWebSocket ws, JsonSerializerSettings serializerSettings)
        {
            WebSocket = ws;
            _serializerSettings = serializerSettings;
            WebSocket.MessageReceived += WebSocket_MessageReceived;
        }

        private void WebSocket_MessageReceived(object sender, MessageEventArgs e)
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
                Error?.Invoke(
                    this,
                    new ErrorEventArgs(
                        new InvalidMessageException(
                            e.Message, "Error deserializing incoming message.", ex)));
            }

            if (rpcmsg.Id != null)
            {
                HandleResponse(rpcmsg);
            }
            else if (rpcmsg.Event != null)
            {
                HandleEvent(rpcmsg);
            }
            else
            {
                throw new InvalidOperationException($"Invalid message from server: {msg}");
            }
        }

        private void HandleResponse(MessageDto message)
        {
            try
            {
                RpcRequest req = ConsumeRequest(message.Id ?? default);

                if (message.Error != null)
                {
                    req.Callback(null, message.Error);
                }
                else
                {
                    req.Callback(new RequestResult
                    {
                        Result = message.Result
                    }, null);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        private void HandleEvent(MessageDto message)
        {
            try
            {
                // Event
                var idx = message.Event.LastIndexOf('.');
                if (idx < 0 || idx == message.Event.Length - 1)
                {
                    throw new InvalidOperationException($"Malformed event name: {message.Event}");
                }
                var rid = message.Event.Substring(0, idx);

                ResourceEvent?.Invoke(this, new ResourceEventArgs
                {
                    ResourceID = rid,
                    EventName = message.Event.Substring(idx + 1),
                    Data = message.Data,
                });
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        public void Request(string method, object parameters, ResponseCallback callback)
        {
            RpcRequest req;
            lock (_requestLock)
            {
                if (_requests == null)
                {
                    Task.Run(() => callback(null, new ResError(ResError.CodeConnectionError, "Connection closed")));
                    return;
                }

                req = new RpcRequest
                {
                    Id = _requestId++,
                    Method = method,
                    Params = parameters,
                    Callback = callback,
                };
                _requests.Add(req.Id, req);
            }

            var dta = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req, _serializerSettings));

            Task.Run(async () =>
            {
                try
                {
                    await this.WebSocket.SendAsync(dta);
                }
                catch (ResException e)
                {
                    ConsumeRequest(req.Id);
                    callback(null, e.Error);
                }
                catch (Exception e)
                {
                    ConsumeRequest(req.Id);
                    callback(null, new ResError(e.Message));
                }
            });
        }

        private RpcRequest ConsumeRequest(int id)
        {
            lock (_requestLock)
            {
                if (this._requests == null)
                {
                    throw new InvalidOperationException($"Incoming request disposed: {id}");
                }

                if (this._requests.TryGetValue(id, out RpcRequest req))
                {
                    this._requests.Remove(id);
                    return req;
                }
            }

            throw new InvalidOperationException($"Invalid incoming request ID: {id}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
            {
                // Call all pending requests with a connection closed error.
                Dictionary<int, RpcRequest> pendingRequests;
                lock (_requestLock)
                {
                    pendingRequests = this._requests;
                    this._requests = null;
                }
                foreach (var req in pendingRequests)
                {
                    Task.Run(() => req.Value.Callback(null, new ResError(ResError.CodeConnectionError, "Connection closed")));
                }

                WebSocket.Dispose();
            }
            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
