using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{

    public delegate ResModelResource ModelFactory(ResClient client, string rid);

    class ResourceTypeModel : IResourceType
    {
        public ResourceType ResourceType { get { return ResourceType.Model; } }

        public string ResourceProperty { get { return "models"; } }

        public readonly PatternMap<ModelFactory> Patterns;

        private ItemCache cache;

        public ResourceTypeModel(ItemCache cache)
        {
            this.cache = cache;
            Patterns = new PatternMap<ModelFactory>(defaultModelFactory);
        }

        public ResResource CreateResource(string rid)
        {
            ModelFactory f = Patterns.Get(rid);
            return f(cache.Client, rid);
        }

        public ResourceEventArgs HandleEvent(object resource, ResourceEventArgs ev)
        {
            switch (ev.EventName)
            {
                case "change":
                    return handleChangeEvent(resource, ev);
            }
            return ev;
        }

        private ResourceEventArgs handleChangeEvent(object resource, ResourceEventArgs ev)
        {
            // Cache new resources if available
            cache.AddResources(ev.Data);

            var model = (Dictionary<string, object>)resource;

            JObject data = ev.Data as JObject;
            if (data == null)
            {
                throw new InvalidOperationException("Model data is not a json object.");
            }

            JObject obj = data["values"] as JObject;
            if (obj == null)
            {
                throw new InvalidOperationException("Change event values propertly is not a json object.");
            }

            var indirect = new Dictionary<string, int>();
            var newProps = new Dictionary<string, object>(obj.Count);
            var oldProps = new Dictionary<string, object>(obj.Count);
            foreach (JProperty prop in obj.Properties())
            {
                var newValue = cache.ParseValue(prop.Value, false);
                if (newValue == ResAction.Delete)
                {
                    // Try delete property
                    if (model.TryGetValue(prop.Name, out var oldValue))
                    {
                        newProps[prop.Name] = ResAction.Delete;
                        oldProps[prop.Name] = oldValue;
                        model.Remove(prop.Name);
                        modifyIndirect(indirect, oldValue, -1);
                    }
                }
                else
                {
                    // Try update property
                    if (model.TryGetValue(prop.Name, out var oldValue))
                    {
                        if (!Object.Equals(oldValue, newValue))
                        {
                            newProps[prop.Name] = newValue;
                            oldProps[prop.Name] = oldValue;
                            model[prop.Name] = newValue;
                            modifyIndirect(indirect, oldValue, -1);
                            modifyIndirect(indirect, newValue, 1);
                        }
                    }
                    else
                    {
                        newProps[prop.Name] = newValue;
                        oldProps[prop.Name] = ResAction.Delete;
                        model[prop.Name] = newValue;
                        modifyIndirect(indirect, newValue, 1);
                    }
                }
            }

            // If no properties were changed, trigger no event.
            if (newProps.Count == 0)
            {
                return null;
            }

            // Remove indirect reference to resources no longer referenced in the model
            foreach (KeyValuePair<string, int> pair in indirect)
            {
                if (pair.Value != 0)
                {
                    var ci = cache.GetItem(pair.Key);

                    ci.AddReference(pair.Value);
                    if (pair.Value < 0)
                    {
                        cache.TryDelete(ci);
                    }
                }
            }

            return new ModelChangeEventArgs
            {
                ResourceID = ev.ResourceID,
                EventName = ev.EventName,
                Data = ev.Data,
                NewValues = newProps,
                OldValues = oldProps,
            };
        }

        public object InitResource(ResResource resource, JToken data)
        {
            ResModelResource model = resource as ResModelResource;
            if (model == null)
            {
                throw new InvalidOperationException("Resource not implementing IResModel.");
            }

            JObject obj = data as JObject;
            if (obj == null)
            {
                throw new InvalidOperationException("Model data is not a json object.");
            }

            var props = new Dictionary<string, object>(obj.Count);
            foreach (JProperty prop in obj.Properties())
            {
                props[prop.Name] = cache.ParseValue(prop.Value, true);
            }

            model.Init(props);

            return props;
        }

        public ResourceEventArgs[] SynchronizeResource(object resource, JToken data)
        {
            throw new NotImplementedException();
        }

        private ResModelResource defaultModelFactory(ResClient client, string rid)
        {
            return new ResModel(client, rid);
        }

        private void modifyIndirect(Dictionary<string, int> indirect, object value, int diff)
        {
            var resource = value as ResResource;
            if (resource != null)
            {
                indirect[resource.ResourceID] = (indirect.TryGetValue(resource.ResourceID, out var c) ? c : 0) + diff;
            }
        }
    }
}
