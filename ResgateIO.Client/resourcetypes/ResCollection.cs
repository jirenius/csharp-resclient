using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES collection.
    /// </summary>
    public class ResCollection<T> : ResResource, IResCollection, IReadOnlyList<T>
    {
        private List<T> values = null;

        /// <summary>
        /// Resource type.
        /// </summary>
        public ResourceType ResourceType { get { return ResourceType.Collection; } }

        public int Count => ((IReadOnlyCollection<T>)values).Count;

        public T this[int index] => ((IReadOnlyList<T>)values)[index];

        /// <summary>
        /// Initializes a new instance of the ResCollection class.
        /// </summary>
        public ResCollection(string rid) : base(rid) { }

        /// <summary>
        /// Initializes the collection with values.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="values">Collection values.</param>
        public void Init(IReadOnlyList<object> values)
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
        public void HandleAdd(int index, object value)
        {
            this.values.Insert(index, (T)value);
            // [TODO] Trigger observables
        }

        /// <summary>
        /// Handles a remove event by removing value from the collection.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="index">Index position of the removed value.</param>
        public void HandleRemove(int index)
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
        public ResCollection(string rid) : base(rid) { }
    }
}
