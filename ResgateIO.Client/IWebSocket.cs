using System;
using System.Threading.Tasks;

namespace ResgateIO.Client
{
    public interface IWebSocket : IDisposable
    {
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
        Task SendAsync(byte[] data);
        Task DisconnectAsync();
    }
}

