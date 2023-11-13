using System;

namespace ResgateIO.Client
{
    public class ConnectionStatusEventArgs : EventArgs
    {
        public ConnectionStatus ConnectionStatus { get; }

        public ConnectionStatusEventArgs(ConnectionStatus connectionStatus)
        {
            ConnectionStatus = connectionStatus;
        }
    }
}