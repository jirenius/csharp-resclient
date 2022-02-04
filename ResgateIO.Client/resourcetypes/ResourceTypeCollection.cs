using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
    public delegate IResCollection CollectionFactory(ResClient client, string rid);

    class ResourceTypeCollection : IResourceType
    {
        public ResourceType ResourceType { get { return ResourceType.Collection; } }

        public string ResourceProperty { get { return "collections"; } }

        private ResClient client;

        public ResourceTypeCollection(ResClient client)
        {
            this.client = client;
        }

        public ResResource CreateResource(string rid)
        {
            return new ResCollection(rid);
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
