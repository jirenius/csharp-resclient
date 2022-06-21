using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    /// <summary>
    /// Initializes a mock connection, a test logger, and a service with the name "test".
    /// </summary>
    public abstract class TestsBase : IDisposable
    {
        public readonly ITestOutputHelper Output;
        public readonly ResClient Client;
        private readonly Queue<ErrorEventArgs> Errors = new Queue<ErrorEventArgs>();
        private readonly Queue<TaskCompletionSource<ErrorEventArgs>> nextErrorTasks = new Queue<TaskCompletionSource<ErrorEventArgs>>();
        public readonly object errorsLock = new object();

        public MockWebSocket WebSocket { get; private set; }
        public MockResgate Resgate { get; private set; }

        public TestsBase(ITestOutputHelper output)
        {
            Output = output;
            Client = new ResClient(createWebSocket);
            Client.Error += onError;
            var converter = new Converter(output);
            Console.SetOut(converter);
        }

        private void onError(object sender, ErrorEventArgs e)
        {
            lock (errorsLock)
            {
                Errors.Enqueue(e);
                Output.WriteLine(String.Format("[ERROR] {0}", e.GetException().ToString()));
                tryCompleteNextError();
            }

        }

        public async Task ConnectAndHandshake(string protocol = "1.2.2")
        { 
            var connectTask = Client.ConnectAsync();
            await Resgate.HandshakeAsync(protocol);
            await connectTask;

            Assert.Equal(protocol, Client.ResgateProtocol);
        }

        private Task<IWebSocket> createWebSocket()
        {
            WebSocket = new MockWebSocket(Output);
            Resgate = new MockResgate(WebSocket);
            return Task.FromResult<IWebSocket>(WebSocket);
        }

        private bool tryCompleteNextError()
        {
            if (Errors.Count == 0 || nextErrorTasks.Count == 0)
            {
                return false;
            }

            var ev = Errors.Dequeue();
            var task = nextErrorTasks.Dequeue();

            task.SetResult(ev);
            return true;
        }

        public async Task<ErrorEventArgs> NextError()
        {
            Task<ErrorEventArgs> task = null;
            TaskCompletionSource<ErrorEventArgs> completionSource = null;
            lock (errorsLock)
            {
                completionSource = new TaskCompletionSource<ErrorEventArgs>();
                nextErrorTasks.Enqueue(completionSource);

                if (nextErrorTasks.Count == 1 && tryCompleteNextError())
                {
                    task = completionSource.Task;
                }
            }

            if (task != null)
            {
                return await task;
            }


            if (completionSource.Task != await Task.WhenAny(completionSource.Task, Task.Delay(1000)))
            {
                // Timeout
                lock (errorsLock)
                {
                    // Additional assertion
                    if (nextErrorTasks.Peek() == completionSource)
                    {
                        nextErrorTasks.Dequeue();
                    }
                }
                throw new TimeoutException();
            }

            return await completionSource.Task;

        }

        public void Dispose()
        {
            Client.Error -= onError;
            Client.Dispose();
            WebSocket?.Dispose();
            // Final assertion
            Assert.Empty(Errors);
        }
    }

    class Converter : TextWriter
    {
        ITestOutputHelper _output;
        public Converter(ITestOutputHelper output)
        {
            _output = output;
        }
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
        public override void WriteLine(string message)
        {
            _output.WriteLine(message);
        }
        public override void WriteLine(string format, params object[] args)
        {
            _output.WriteLine(format, args);
        }

        public override void Write(char value)
        {
            throw new NotSupportedException("This text writer only supports WriteLine(string) and WriteLine(string, params object[]).");
        }
    }
}
