using System;

namespace ResgateIO.Client
{
    public class ResException : Exception
    {
        public ResError Error { get; }
        public string Code { get { return Error.Code; } }
        public object ErrorData { get { return Error.Data; } }

        public ResException()
        {
            Error = new ResError();
        }

        public ResException(string message)
            : base(message)
        {
            Error = new ResError(message);
        }

        public ResException(string message, Exception inner)
            : base(message, inner)
        {
            Error = new ResError(message);
        }

        public ResException(string code, string message)
            : base(message)
        {
            Error = new ResError(code, message);
        }

        public ResException(string code, string message, Exception inner)
            : base(message, inner)
        {
            Error = new ResError(code, message);
        }

        public ResException(string code, string message, object errorData)
            : base(message)
        {
            Error = new ResError(code, message, errorData);
        }

        public ResException(string code, string message, object errorData, Exception inner)
            : base(message, inner)
        {
            Error = new ResError(code, message, errorData);
        }

        public ResException(ResError error)
            : base(error.Message)
        {
            Error = error;
        }

        public ResException(ResError error, Exception inner)
            : base(error.Message, inner)
        {
            Error = error;
        }
    }
}
