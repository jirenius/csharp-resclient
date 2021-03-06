using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace ResgateIO.Client
{
    public class WebSocket : IWebSocket
    {
        public int ReceiveBufferSize = 8192;
        public event EventHandler<MessageEventArgs> OnMessage;
        public event EventHandler OnClose;

        private ClientWebSocket ws;
        private CancellationTokenSource cts;

        public WebSocket()
        {
        }

        public async Task ConnectAsync(string url)
        {
            if (ws != null)
            {
                if (ws.State == WebSocketState.Open)
                {
                    return;
                }
                else
                {
                    ws.Dispose();
                }
            }

            ws = new ClientWebSocket();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            await ws.ConnectAsync(new Uri(url), cts.Token);
            await Task.Factory.StartNew(ReceiveLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task DisconnectAsync()
        {
            if (ws == null)
            {
                return;
            }
            
            if (ws.State == WebSocketState.Open)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            ws.Dispose();
            ws = null;
            cts?.Dispose();
            cts = null;
        }

        public async Task SendAsync(byte[] data)
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket not connected.");
            }

            await ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task ReceiveLoop()
        {
            WebSocketReceiveResult receiveResult = null;
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    using (var inputStream = new MemoryStream(ReceiveBufferSize))
                    {
                        do
                        {
                            receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                            if (receiveResult.MessageType != WebSocketMessageType.Close)
                                inputStream.Write(buffer, 0, receiveResult.Count);
                        }
                        while (!receiveResult.EndOfMessage);

                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        MessageReceived(inputStream.ToArray());
                    }
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                OnClose?.Invoke(this, EventArgs.Empty);
            }
        }

        private void MessageReceived(byte[] msg)
        {
            OnMessage?.Invoke(this, new MessageEventArgs
            {
                Message = msg
            });
        }

        public void Dispose()
        {
            if (ws != null)
            {
                if (ws.State == WebSocketState.Open)
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(2));
                    ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                try { if (ws != null) { ws.Dispose(); ws = null; } } catch (Exception) { }
                try { if (cts != null) { cts.Dispose(); cts = null; } } catch (Exception) { }
            }
        }
    }
}
