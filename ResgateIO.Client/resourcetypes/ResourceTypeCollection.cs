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

        private ResClient client;

        public ResourceTypeCollection(ResClient client)
        {
            this.client = client;
            Patterns = new PatternMap<CollectionFactory>(defaultCollectionFactory);
        }

        public ResResource CreateResource(string rid)
        {
            CollectionFactory f = Patterns.Get(rid);
            return f(this.client, rid);
        }

        private ResCollectionResource defaultCollectionFactory(ResClient client, string rid)
        {
            return new ResCollection(client, rid);
        }

        public void InitResource(ResResource resource, JToken data)
        {
            ResCollection Collection = resource as ResCollection;
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
                values.Add(client.ParseValue(value, true));
            }

            Collection.Init(values);
        }

        public void SynchronizeResource(ResResource resource, JToken data)
        {
            throw new NotImplementedException();
        }
    }
}
