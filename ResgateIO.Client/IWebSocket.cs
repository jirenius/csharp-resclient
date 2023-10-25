using System;
using System.Threading.Tasks;

namespace ResgateIO.Client
{
    public interface IWebSocket : IDisposable
    {
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler OnClose;
        Task SendAsync(byte[] data);
        Task DisconnectAsync();
    }
}

