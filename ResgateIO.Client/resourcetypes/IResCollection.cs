using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{

    public interface IResCollection
    {

        /// <summary>
        /// Initializes the collection with values.
        /// </summary>
        /// <param name="values">Collection values.</param>
        void Init(IReadOnlyList<object> values);

        /// <summary>
        /// Handles an add event by adding a value to the collection.
        /// </summary>
        /// <param name="index">Index position of the added value.</param>
        /// <param name="value">Value being added.</param>
        void HandleAdd(int index, object value);

        /// <summary>
        /// Handles a remove event by removing value from the collection.
        /// </summary>
        /// <param name="index">Index position of the removed value.</param>
        void HandleRemove(int index);
    }
}
