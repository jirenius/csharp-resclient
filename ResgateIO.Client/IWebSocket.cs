using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ResgateIO.Client
{
    public interface IWebSocket : IDisposable
    {
        event EventHandler<MessageEventArgs> OnMessage;
        event EventHandler OnClose;
        Task SendAsync(byte[] data);
        
        Task DisconnectAsync();
    }
}


public class MessageEventArgs : EventArgs
{
    public byte[] Message { get; set; }
}
