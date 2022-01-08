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
        /// Resource ID.
        /// </summary>
        public string ResourceID { get; private set; }

        /// <summary>
        /// Name of the resource type.
        /// </summary>
        public ResourceType Type { get; }

        /// <summary>
        /// Initializes a new instance of the ResResource class.
        /// </summary>
        public ResResource(string rid)
        {
            ResourceID = rid;
        }
    }
}