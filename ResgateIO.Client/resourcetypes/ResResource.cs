using System;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES resource.
    /// </summary>
    public abstract class ResResource
    {
        /// <summary>
        /// Resource type.
        /// </summary>
        public abstract ResourceType Type { get; }

        /// <summary>
        /// Resource ID.
        /// </summary>
        public string ResourceID { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ResResource class.
        /// </summary>
        public ResResource(string rid)
        {
            ResourceID = rid;
        }
    }
}