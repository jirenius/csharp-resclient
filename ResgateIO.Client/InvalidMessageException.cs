using Newtonsoft.Json.Linq;
using System;

namespace ResgateIO.Client
{
    public class InvalidMessageException : Exception
    {
        public byte[] RawMessage { get; }

        public InvalidMessageException()
        {
        }

        public InvalidMessageException(string message)
            : base(message)
        {
        }

        public InvalidMessageException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public InvalidMessageException(byte[] rawMessage, string message, Exception inner)
            : base(message, inner)
        {
            RawMessage = rawMessage;
        }
    }
}
