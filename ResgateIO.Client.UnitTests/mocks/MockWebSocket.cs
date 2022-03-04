using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class MockWebSocket : IWebSocket
    {
        public event EventHandler<MessageEventArgs> OnMessage;

        private readonly ITestOutputHelper log;
        private HashSet<int> requestIds = new HashSet<int>();
        private Queue<MockRequest> requestQueue = new Queue<MockRequest>();
        private Queue<TaskCompletionSource<MockRequest>> awaiters = new Queue<TaskCompletionSource<MockRequest>>();
        private object requestLock = new object();

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


            tcs = awaiters.Dequeue();
            tcs.SetResult(request);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            log?.WriteLine("<-X Disconnected");
            return Task.CompletedTask;
        }

        public void SendMessage(byte[] data)
        {
            log?.WriteLine("--> {0}", Encoding.UTF8.GetString(data));
            OnMessage.Invoke(this, new MessageEventArgs
            {
                Message = data,
            });
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
