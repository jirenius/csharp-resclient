using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ResRpcTests
    {
        public readonly ITestOutputHelper Output;

        public ResRpcTests(ITestOutputHelper output)
        {
            Output = output;
        }

        [Fact]
        public async void ResRpc_TaskCompletionSource_RunsContinuationTaskOnSameThread()
        {
            bool invoked = false;
            var tcs = new TaskCompletionSource<string>(TaskContinuationOptions.ExecuteSynchronously);
            var task = Task.Run(async () =>
            {
                Output.WriteLine("Awaiting task on thread ID: {0}", Thread.CurrentThread.ManagedThreadId);
                await tcs.Task;
                Output.WriteLine("Continuation called on thread ID: {0}", Thread.CurrentThread.ManagedThreadId);
                invoked = true;
            });
            await Task.Delay(10);
            Output.WriteLine("SetResult called on thread ID: {0}", Thread.CurrentThread.ManagedThreadId);
            tcs.SetResult("done");
            Assert.True(invoked);
        }
    }
}
