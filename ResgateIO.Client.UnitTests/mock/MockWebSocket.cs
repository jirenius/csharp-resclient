using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResgateIO.Client.UnitTests
{
    class MockWebSocket : IWebSocket
    {
        public event EventHandler<MessageEventArgs> OnMessage;

        private HashSet<int> requestIds = new HashSet<int>();
        private Queue<MockRequest> requestQueue = new Queue<MockRequest>();
        private Queue<TaskCompletionSource<MockRequest>> awaiters = new Queue<TaskCompletionSource<MockRequest>>();
        private object requestLock = new object();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(byte[] data)
        {
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

        public void SendMessage(byte[]dta)
        {
            OnMessage.Invoke(this, new MessageEventArgs
            {
                Message = dta,
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
