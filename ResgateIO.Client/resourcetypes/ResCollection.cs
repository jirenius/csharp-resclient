using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES collection.
    /// </summary>
    public class ResCollection : ResResource, IReadOnlyList<object>
    {
        private List<object> values = null;

        public int Count => ((IReadOnlyCollection<object>)values).Count;

        public object this[int index] => ((IReadOnlyList<object>)values)[index];

        /// <summary>
        /// Initializes a new instance of the ResCollection class.
        /// </summary>
        public ResCollection(string rid) : base(rid) {}

        /// <summary>
        /// Initializes the collection with value.
        /// </summary>
        public void Init(List<object> values)
        {
            this.values = values;
        }

        public void HandleAdd(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void HandleRemove(int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<object> GetEnumerator()
        {
            return ((IEnumerable<object>)values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)values).GetEnumerator();
        }
    }
}