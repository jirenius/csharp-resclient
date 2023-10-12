using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResgateIO.Client
{

    /// <summary>
    /// Represents a RES resource error.
    /// </summary>
    public class ResResourceError : ResResource
    {
        /// <summary>
        /// Resource type.
        /// </summary>
        public override ResourceType Type { get { return ResourceType.Error; } }

        /// <summary>
        /// Initializes a new instance of the ResErrorResource class.
        /// </summary>
        public ResResourceError(string rid) : base(rid) { }

        /// <summary>
        /// Handles incoming events.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="ev">Resource event.</param>
        public override void HandleEvent(ResourceEventArgs ev)
        {
            throw new InvalidOperationException("Event on error resource " + this.ResourceID +": " + ev.EventName);
        }
    }
}