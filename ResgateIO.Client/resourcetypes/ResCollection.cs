using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES collection.
    /// </summary>
    public class ResCollection : ResResource
    {
        public IReadOnlyList<object> Values { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ResCollection class.
        /// </summary>
        public ResCollection(string rid) : base(rid) {}

        /// <summary>
        /// Initializes the collection with value.
        /// </summary>
        public void Init(List<object> values)
        {
            Values = values;
        }

        public void HandleAdd(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void HandleRemove(int index)
        {
            throw new NotImplementedException();
        }
    }
}