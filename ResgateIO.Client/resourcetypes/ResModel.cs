using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES model.
    /// </summary>
    public class ResModel : ResResource
    {
        public IReadOnlyDictionary<string, object> Props { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ResModel class.
        /// </summary>
        public ResModel(string rid) : base(rid) {}

        /// <summary>
        /// Initializes the model with values.
        /// </summary>
        public void Init(Dictionary<string, object> props)
        {
            Props = props;
        }

        /// <summary>
        /// Handles update events.
        /// </summary>
        /// <param name="newProps">New property values.</param>
        public void HandleUpdate(IReadOnlyDictionary<string, object> newProps)
        {
            throw new NotImplementedException();
        }
    }
}