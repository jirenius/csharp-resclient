using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES collection.
    /// </summary>
    public class ResCollection<T> : ResCollectionResource, IReadOnlyList<T>
    {
        private List<T> values = null;

        /// <summary>
        /// Resource type.
        /// </summary>
        public ResourceType ResourceType { get { return ResourceType.Collection; } }

        public int Count => ((IReadOnlyCollection<T>)values).Count;

        public T this[int index] => ((IReadOnlyList<T>)values)[index];

        public readonly ResClient Client;

        /// <summary>
        /// Initializes a new instance of the ResCollection class.
        /// </summary>
        public ResCollection(ResClient client, string rid) : base(rid)
        {
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
        public Task<TResult> CallAsync<TResult>(string method, object parameters)
        {
            return Client.CallAsync<TResult>(ResourceID, method, parameters);
        }

        /// <summary>
        /// Sends a request to a call method and returns the result as a value of type T.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<TResult> CallAsync<TResult>(string method)
        {
            return Client.CallAsync<TResult>(ResourceID, method, null);
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
        public Task<TResult> AuthAsync<TResult>(string method, object parameters)
        {
            return Client.AuthAsync<TResult>(ResourceID, method, parameters);
        }

        /// <summary>
        /// Sends a request to an authentication method and returns the result as a value of type T.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <returns>The result.</returns>
        public Task<TResult> AuthAsync<TResult>(string method)
        {
            return Client.AuthAsync<TResult>(ResourceID, method);
        }

        /// <summary>
        /// Initializes the collection with values.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="values">Collection values.</param>
        public override void Init(IReadOnlyList<object> values)
        {
            this.values = new List<T>(values.Count);
            foreach (var value in values)
            {
                this.values.Add((T)value);
            }
        }

        /// <summary>
        /// Handles an add event by adding a value to the collection.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="index">Index position of the added value.</param>
        /// <param name="value">Value being added.</param>
        public override void HandleAdd(int index, object value)
        {
            this.values.Insert(index, (T)value);
            // [TODO] Trigger observables
        }

        /// <summary>
        /// Handles a remove event by removing value from the collection.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="index">Index position of the removed value.</param>
        public override void HandleRemove(int index)
        {
            this.values.RemoveAt(index);
            // [TODO] Trigger observables
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)values).GetEnumerator();
        }
    }

    public class ResCollection : ResCollection<object>
    {
        public ResCollection(ResClient client, string rid) : base(client, rid) { }
    }
}
