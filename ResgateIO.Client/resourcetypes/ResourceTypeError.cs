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

        public readonly PatternMap<ResourceFactory> Patterns;

        // Events
        public event ErrorEventHandler Error;

        private ItemCache cache;

        public ResourceTypeError(ItemCache cache, PatternMap<ResourceFactory> resourcePatterns)
        {
            this.cache = cache;
            Patterns = resourcePatterns;
        }

        public ResResource CreateResource(string rid)
        {
            // If a resource factory is registered for the pattern, we create
            // an instance of that specific resource instead of a ResResourceError.
            if (Patterns.TryGet(rid, out ResourceFactory factory))
            {
                if (factory.ModelFactory != null)
                {
                    return (ResResource)factory.ModelFactory(cache.Client, rid);
                }
                else if (factory.CollectionFactory != null)
                {
                    return (ResResource)factory.CollectionFactory(cache.Client, rid);
                }
            }
            return new ResResourceError(rid);
        }

        public ResourceEventArgs HandleEvent(object resource, ResourceEventArgs ev)
        {
            return ev;
        }

        public object InitResource(ResResource resource, JToken data)
        {
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

            try
            {
                resource.Init(err);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }

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
