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
            switch (ev.EventName)
            {
                case "add":
                    return handleAddEvent(resource, ev);
                case "remove":
                    return handleRemoveEvent(resource, ev);
            }
            return ev;
        }

        private ResourceEventArgs handleAddEvent(object resource, ResourceEventArgs ev)
        {
            // Cache new resources if available
            cache.AddResources(ev.Data);

            var collection = (List<object>)resource;

            JObject data = ev.Data as JObject;
            if (data == null)
            {
                throw new InvalidOperationException("Add event data is not a json object.");
            }

            JToken valueToken = data["value"];
            if (valueToken == null)
            {
                throw new InvalidOperationException("Add event missing value property.");
            }
            JToken idxToken = data["idx"];
            if (idxToken == null)
            {
                throw new InvalidOperationException("Add event missing idx property.");
            }
            int idx = idxToken.Value<int>();

            var value = cache.ParseValue(valueToken, true);

            collection.Insert(idx, value);

            return new CollectionAddEventArgs
            {
                ResourceID = ev.ResourceID,
                EventName = ev.EventName,
                Data = ev.Data,
                Index = idx,
                Value = value,
            };
        }

        private ResourceEventArgs handleRemoveEvent(object resource, ResourceEventArgs ev)
        {
            var collection = (List<object>)resource;

            JObject data = ev.Data as JObject;
            if (data == null)
            {
                throw new InvalidOperationException("Remove event data is not a json object.");
            }

            JToken idxToken = data["idx"];
            if (idxToken == null)
            {
                throw new InvalidOperationException("Remove event missing idx property.");
            }
            int idx = idxToken.Value<int>();

            var value = collection[idx];
            collection.RemoveAt(idx);

            var resresource = value as ResResource;
            if (resresource != null)
            {
                var ci = cache.GetItem(resresource.ResourceID);
                ci.AddReference(-1);
                cache.TryDelete(ci);
            };

            return new CollectionRemoveEventArgs
            {
                ResourceID = ev.ResourceID,
                EventName = ev.EventName,
                Data = ev.Data,
                Index = idx,
                Value = value,
            };
        }

        public ResourceEventArgs[] SynchronizeResource(object resource, JToken data)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetResourceValues(object resource)
        {
            return (List<object>)resource;
        }
    }
}
