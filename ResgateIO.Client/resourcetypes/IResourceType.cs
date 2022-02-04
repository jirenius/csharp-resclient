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
        /// Initializes a ResResource created with CreateResource..
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <param name="data">Resource data.</param>
        void InitResource(ResResource resource, JToken data);

        /// <summary>
        /// Synchronize a ResResource created with CreateResource.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <param name="data">Resource data.</param>
        void SynchronizeResource(ResResource resource, JToken data);
    }
}
