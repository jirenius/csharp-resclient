using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{

    public interface IResModel
    {

        /// <summary>
        /// Initializes the model with property values.
        /// </summary>
        /// <param name="props">All model property values.</param>
        void Init(IReadOnlyDictionary<string, object> props);

        /// <summary>
        /// Handles a change event by updating the model with changed property values.
        /// </summary>
        /// <param name="props">Changed properties and their new value.</param>
        void HandleChange(IReadOnlyDictionary<string, object> props);
    }
}
