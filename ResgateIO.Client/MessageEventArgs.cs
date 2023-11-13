using System;

namespace ResgateIO.Client
{
    public class MessageEventArgs : EventArgs
    {
        public byte[] Message { get; }

        public MessageEventArgs(byte[] message)
        {
            Message = message;
        }
    }
}