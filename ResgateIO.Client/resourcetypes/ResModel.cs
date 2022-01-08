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
        public Dictionary<string, object> Props { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ResModel class.
        /// </summary>
        public ResModel(string rid) : base(rid) {}

        /// <summary>
        /// Initializes a the values of a ResModelclass.
        /// </summary>
        public void Init(Dictionary<string, object> props)
        {
            Props = props;
        }
    }
}