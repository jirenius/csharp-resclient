using System;

namespace ResgateIO.Client
{
    public class ResEventException : Exception
    {
        public ResourceEventArgs ResourceEvent { get; }

        public ResEventException()
        {
        }

        public ResEventException(string message)
            : base(message)
        {
        }

        public ResEventException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public ResEventException(ResourceEventArgs resourceEvent, string message)
            : base(message)
        {
            ResourceEvent = resourceEvent;
        }

        public ResEventException(ResourceEventArgs resourceEvent, string message, Exception inner)
            : base(message, inner)
        {
            ResourceEvent = resourceEvent;
        }
    }
}
