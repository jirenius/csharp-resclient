using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResgateIO.Client
{

    public class ChangeEventArgs : EventArgs
    {
        public IReadOnlyDictionary<string, object> OldProps { get; set; }
        public IReadOnlyDictionary<string, object> NewProps { get; set; }
    }

    /// <summary>
    /// Represents a RES model.
    /// </summary>
    public class ResModel : ResModelResource, IReadOnlyDictionary<string, object>
    {
        private Dictionary<string, object> props = null;
        public event EventHandler<ChangeEventArgs> ChangeEvent;

        /// <summary>
        /// Resource type.
        /// </summary>
        public ResourceType ResourceType { get { return ResourceType.Model; } }

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, object>)props).Keys;

        public IEnumerable<object> Values => ((IReadOnlyDictionary<string, object>)props).Values;

        public int Count => ((IReadOnlyCollection<KeyValuePair<string, object>>)props).Count;

        public object this[string key] => ((IReadOnlyDictionary<string, object>)props)[key];

        public readonly ResClient Client;

        /// <summary>
        /// Initializes a new instance of the ResModel class.
        /// </summary>
        public ResModel(ResClient client, string rid) : base(rid) {
            Client = client;
        }

        /// <summary>
        /// Sends a request to a call method.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<object> CallAsync(string method, object parameters)
        {
            return Client.CallAsync(ResourceID, method, parameters);
        }

        /// <summary>
        /// Sends a request to a call method.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<object> CallAsync(string method)
        {
            return Client.CallAsync(ResourceID, method);
        }

        /// <summary>
        /// Sends a request to a call method and returns the result as a value of type T.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<T> CallAsync<T>(string method, object parameters)
        {
            return Client.CallAsync<T>(ResourceID, method, parameters);
        }

        /// <summary>
        /// Sends a request to a call method and returns the result as a value of type T.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<T> CallAsync<T>(string method)
        {
            return Client.CallAsync<T>(ResourceID, method, null);
        }

        /// <summary>
        /// Sends a request to an authentication method.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<object> AuthAsync(string method, object parameters)
        {
            return Client.AuthAsync(ResourceID, method, parameters);
        }

        /// <summary>
        /// Sends a request to an authentication method.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<object> AuthAsync(string method)
        {
            return Client.AuthAsync(ResourceID, method);
        }

        /// <summary>
        /// Sends a request to an authentication method and returns the result as a value of type T.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>The result.</returns>
        public Task<T> AuthAsync<T>(string method, object parameters)
        {
            return Client.AuthAsync<T>(ResourceID, method, parameters);
        }

        /// <summary>
        /// Sends a request to an authentication method and returns the result as a value of type T.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<T> AuthAsync<T>(string method)
        {
            return Client.AuthAsync<T>(ResourceID, method);
        }

        /// <summary>
        /// Initializes the model with property values.
        /// </summary>
        /// <remarks>Not to be called directly. Called by ResClient.</remarks>
        /// <param name="props">All model property values.</param>
        public override void Init(IReadOnlyDictionary<string, object> props)
        {
            this.props = new Dictionary<string, object>(props.Count);
            foreach (var pair in props)
            {
                this.props.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Updates the model with changed property values.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="props">Changed properties and their new value.</param>
        public override void HandleChange(IReadOnlyDictionary<string, object> props)
        {
            var oldProps = new Dictionary<string, object>(props.Count);
            foreach (var pair in props)
            {
                if (this.props.ContainsKey(pair.Key))
                {
                    oldProps.Add(pair.Key, this.props[pair.Key]);
                }
                if (pair.Value == ResAction.Delete)
                {
                    this.props.Remove(pair.Key);
                }
                else
                {
                    this.props.Add(pair.Key, pair.Value);
                }
            }

            EventHandler<ChangeEventArgs> handler = ChangeEvent;
            if (handler != null)
            {
                handler(this, new ChangeEventArgs
                {
                    NewProps = props,
                    OldProps = oldProps
                });
            }
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