using System;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES client error.
    /// </summary>
    /// <see>https://resgate.io/docs/specification/res-service-protocol/#error-object</see>
    public class ResError
    {
        // Predefined error codes
        public const string CodeAccessDenied = "system.accessDenied";
        public const string CodeInternalError = "system.internalError";
        public const string CodeInvalidParams = "system.invalidParams";
        public const string CodeInvalidQuery = "system.invalidQuery";
        public const string CodeMethodNotFound = "system.methodNotFound";
        public const string CodeNotFound = "system.notFound";
        public const string CodeTimeout = "system.timeout";
        public const string CodeInvalidRequest = "system.invalidRequest";

        [JsonProperty(PropertyName = "code")]
        public string Code;

        [JsonProperty(PropertyName = "message")]
        public string Message;

        [JsonProperty(PropertyName = "data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data = null;

        /// <summary>
        /// Initializes a new instance of the ResError class with CodeInternalError and the message "Internal error".
        /// </summary>
        public ResError()
        {
            Code = ResError.CodeInternalError;
            Message = "Internal error";
        }

        /// <summary>
        /// Initializes a new instance of the ResError class with CodeInternalError and custom error message.
        /// </summary>
        /// <param name="message">Error message.</param>
        public ResError(string message)
        {
            Code = ResError.CodeInternalError;
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the ResError class with custom error code and message.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <param name="message">Error message.</param>
        public ResError(string code, string message)
        {
            Code = code;
            Message = message;
        }


        /// <summary>
        /// Initializes a new instance of the ResError class with custom error code, message, and data.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <param name="message">Error message.</param>
        /// <param name="data">Additional data. Must be JSON serializable.</param>
        public ResError(string code, string message, object data)
        {
            Code = code;
            Message = message;
            Data = data;
        }
    }
}