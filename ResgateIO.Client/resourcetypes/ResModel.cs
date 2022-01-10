using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES model.
    /// </summary>
    public class ResModel : ResResource, IReadOnlyDictionary<string, object>
    {
        private Dictionary<string, object> props = null;

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, object>)props).Keys;

        public IEnumerable<object> Values => ((IReadOnlyDictionary<string, object>)props).Values;

        public int Count => ((IReadOnlyCollection<KeyValuePair<string, object>>)props).Count;

        public object this[string key] => ((IReadOnlyDictionary<string, object>)props)[key];

        /// <summary>
        /// Initializes a new instance of the ResModel class.
        /// </summary>
        public ResModel(string rid) : base(rid) {}

        /// <summary>
        /// Initializes the model with values.
        /// </summary>
        public void Init(Dictionary<string, object> props)
        {
            this.props = props;
        }

        /// <summary>
        /// Handles update events.
        /// </summary>
        /// <param name="newProps">New property values.</param>
        public void HandleUpdate(IReadOnlyDictionary<string, object> newProps)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            return ((IReadOnlyDictionary<string, object>)props).ContainsKey(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return ((IReadOnlyDictionary<string, object>)props).TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)props).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)props).GetEnumerator();
        }
    }
}