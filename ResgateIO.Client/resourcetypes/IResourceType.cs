using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
    /// <summary>
    /// Resource types
    /// </summary>
    public enum ResourceType
    {
        Model,
        Collection,
        Error
    }

    internal interface IResourceType
    {
        /// <summary>
        /// Resource type
        /// </summary>
        ResourceType ResourceType { get; }

        /// <summary>
        /// Property name of the resources in a resource response, such as "models", "collections", or "errors".
        /// </summary>
        string ResourceProperty { get; }

        /// <summary>
        /// Creates a ResResource of the given type.
        /// </summary>
        /// <param name="rid">Resource ID.</param>
        /// <returns>Resource of the given type.</returns>
        ResResource CreateResource(string rid);

        /// <summary>
        /// Initializes a ResResource created with CreateResource.
        /// Returns an internal representation of the resource.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <param name="data">Resource data.</param>
        /// <returns>Returns an internal representation of the resource.</returns>
        object InitResource(ResResource resource, JToken data);



        /// <summary>
        /// Handles a resource event and updates the internal resource returned from InitResource if needed.
        /// </summary>
        /// <param name="resource">Resource as returned from InitResource.</param>
        /// <param name="ev">Resource event.</param>
        /// <returns>Returns an the event to pass on to event handlers.</returns>
        ResourceEventArgs HandleEvent(object resource, ResourceEventArgs ev);

        /// <summary>
        /// Synchronize the internal resource returned from InitResource.
        /// </summary>
        /// <param name="resource">Resource as returned from InitResource.</param>
        /// <param name="data">Resource data.</param>
        /// <returns>Returns a sequence of events to produce the new state.</returns>
        ResourceEventArgs[] SynchronizeResource(object resource, JToken data);

        /// <summary>
        /// Gets an values stored in the resource.
        /// </summary>
        /// <param name="resource">Resource as returned from InitResource.</param>
        /// <returns></returns>
        IEnumerable<object> GetResourceValues(object resource);
    }
}
