using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{

    public abstract class ResCollectionResource: ResResource
    {
        /// <summary>
        /// Resource type.
        /// </summary>
        public override ResourceType Type { get { return ResourceType.Collection; } }

        /// <summary>
        /// Initializes a new instance of the ResCollectionResource class.
        /// </summary>
        public ResCollectionResource(string rid) : base(rid) { }

        /// <summary>
        /// Initializes the collection with values.
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
        /// <param name="values">Collection values.</param>
        public abstract void Init(IReadOnlyList<object> values);
    }
}
