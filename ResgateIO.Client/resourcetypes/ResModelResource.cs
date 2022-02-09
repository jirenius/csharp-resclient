using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{

    public abstract class ResModelResource: ResResource
    {
        /// <summary>
        /// Resource type.
        /// </summary>
        public override ResourceType Type { get { return ResourceType.Model; } }

        /// <summary>
        /// Initializes a new instance of the ResModelResource class.
        /// </summary>
        public ResModelResource(string rid) : base(rid) { }

        /// <summary>
        /// Initializes the model with property values.
        /// </summary>
        /// <param name="props">All model property values.</param>
        public abstract void Init(IReadOnlyDictionary<string, object> props);

        /// <summary>
        /// Handles a change event by updating the model with changed property values.
        /// </summary>
        /// <param name="props">Changed properties and their new value.</param>
        public abstract void HandleChange(IReadOnlyDictionary<string, object> props);
    }
}
