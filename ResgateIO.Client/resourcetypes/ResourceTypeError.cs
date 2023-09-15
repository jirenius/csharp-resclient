using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResgateIO.Client
{
    
    class ResourceTypeError : IResourceType
    {
        public ResourceType ResourceType { get { return ResourceType.Error; } }

        public string ResourceProperty { get { return "errors"; } }

        // Events
        public event ErrorEventHandler Error;

        private ItemCache cache;

        public ResourceTypeError(ItemCache cache)
        {
            this.cache = cache;
        }

        public ResResource CreateResource(string rid)
        {
            return new ResResourceError(rid);
        }

        public ResourceEventArgs HandleEvent(object resource, ResourceEventArgs ev)
        {
            return ev;
        }

        public object InitResource(ResResource resource, JToken data)
        {
            ResResourceError rerr = resource as ResResourceError;
            if (rerr == null)
            {
                throw new InvalidOperationException("Resource not of type ResResourceError.");
            }

            JObject obj = data as JObject;
            if (obj == null)
            {
                throw new InvalidOperationException("Error data is not a json object.");
            }

            ResError err;
            try
            {
                err = obj.ToObject<ResError>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deserializing resource error.", ex);
            }

            rerr.Init(err);

            return null;
        }

        public void SynchronizeResource(string rid, object resource, JToken data, Action<ResourceEventArgs> onEvent)
        {
            // No syncronizing of errors
        }

        public IEnumerable<object> GetResourceValues(object resource)
        {
            return null;
        }
    }
}
