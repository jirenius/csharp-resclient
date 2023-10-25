using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class MockWebSocket : IWebSocket
    {
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler OnClose;

        private readonly ITestOutputHelper log;
        private readonly HashSet<int> requestIds = new HashSet<int>();
        private readonly Queue<MockRequest> requestQueue = new Queue<MockRequest>();
        private readonly Queue<TaskCompletionSource<MockRequest>> awaiters = new Queue<TaskCompletionSource<MockRequest>>();
        private readonly object requestLock = new object();
        private bool Closed = false;

        public MockWebSocket(ITestOutputHelper output)
        {
            log = output;
        }

        public void Dispose()
        {
        }

        public Task SendAsync(byte[] data)
        {
            log?.WriteLine("<-- {0}", Encoding.UTF8.GetString(data));

            var request = new MockRequest(this, data);

            TaskCompletionSource<MockRequest> tcs;
            lock (requestLock)
            {
                if (Closed)
                {
                    throw new InvalidOperationException("Connection is closed");
                }

                if (requestIds.Contains(request.Id))
                {
                    throw new InvalidOperationException(String.Format("Request ID {0} sent more than once for the same connection.", request.Id));
                }

                if (awaiters.Count == 0)
                {
                    requestIds.Add(request.Id);
                    requestQueue.Enqueue(request);
                    return Task.CompletedTask;
                }

                tcs = awaiters.Dequeue();
            }

            tcs.SetResult(request);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            lock (requestLock)
            {
                if (Closed)
                {
                    throw new InvalidOperationException("Connection already closed");
                }
                Closed = true;
            }
            log?.WriteLine("<-X Disconnected");
            OnClose?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public void SendMessage(byte[] data)
        {
            log?.WriteLine("--> {0}", Encoding.UTF8.GetString(data));
            MessageReceived.Invoke(this, new MessageEventArgs(data));
        }

        public async Task<MockRequest> GetRequestAsync()
        {
            TaskCompletionSource<MockRequest> tcs;
            lock (requestLock)
            {
                if (requestQueue.Count > 0)
                {
                    var request = requestQueue.Dequeue();
                    return request;
                }

                tcs = new TaskCompletionSource<MockRequest>();
                awaiters.Enqueue(tcs);
            }

            return await tcs.Task;
        }
    }
}
