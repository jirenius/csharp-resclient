using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
    public delegate ResCollectionResource CollectionFactory(ResClient client, string rid);

    class ResourceTypeCollection : IResourceType
    {
        public ResourceType ResourceType { get { return ResourceType.Collection; } }

        public string ResourceProperty { get { return "collections"; } }

        public readonly PatternMap<CollectionFactory> Patterns;

        private ItemCache cache;

        public ResourceTypeCollection(ItemCache cache)
        {
            this.cache = cache;
            Patterns = new PatternMap<CollectionFactory>(defaultCollectionFactory);
        }

        public ResResource CreateResource(string rid)
        {
            CollectionFactory f = Patterns.Get(rid);
            return f(cache.Client, rid);
        }

        private ResCollectionResource defaultCollectionFactory(ResClient client, string rid)
        {
            return new ResCollection(client, rid);
        }

        public object InitResource(ResResource resource, JToken data)
        {
            ResCollectionResource Collection = resource as ResCollectionResource;
            if (Collection == null)
            {
                throw new InvalidOperationException("Resource not of type ResCollection.");
            }

            JArray arr = data as JArray;
            if (arr == null)
            {
                throw new InvalidOperationException("Collection data is not a json array.");
            }

            var values = new List<object>(arr.Count);
            foreach (JToken value in arr)
            {
                values.Add(cache.ParseValue(value, true));
            }

            Collection.Init(values);

            return values;
        }
        public ResourceEventArgs HandleEvent(object resource, ResourceEventArgs ev)
        {
            return ev;
        }
        
        public ResourceEventArgs[] SynchronizeResource(object resource, JToken data)
        {
            throw new NotImplementedException();
        }
    }
}
