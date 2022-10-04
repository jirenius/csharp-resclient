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

        /// <summary>
        /// Handle a custom event not change event by updating the model with changed property values.
        /// </summary>
        /// <remarks>
        /// Values will map to the following types:
        /// * JSON null is null
        /// * JSON string is String
        /// * JSON number is Long
        /// * JSON boolean is Boolean
        /// * Data value is JToken
        /// * Resource reference is the referenced resource (eg. ResModel or ResCollection)
        /// * Soft resource reference is ResRef
        /// </remarks>
        /// <param name="props">Changed properties and their new value.</param>
        public virtual void HandleEvent(ResourceEventArgs ev) { }


    }
}