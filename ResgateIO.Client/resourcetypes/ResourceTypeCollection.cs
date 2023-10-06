using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResgateIO.Client
{
    public delegate ResCollectionResource CollectionFactory(ResClient client, string rid);

    class ResourceTypeCollection : IResourceType
    {
        public ResourceType ResourceType { get { return ResourceType.Collection; } }

        public string ResourceProperty { get { return "collections"; } }

        public readonly PatternMap<ResourceFactory> Patterns;

        // Events
        public event ErrorEventHandler Error;

        private ItemCache cache;

        private struct AddValue
        {
            public int ItemIdx;
            public int IdxOffset;
        }

        public ResourceTypeCollection(ItemCache cache, PatternMap<ResourceFactory> resourcePatterns)
        {
            this.cache = cache;
            Patterns = resourcePatterns;
        }

        public ResResource CreateResource(string rid)
        {
            CollectionFactory collectionFactory = defaultCollectionFactory;
            if (Patterns.TryGet(rid, out ResourceFactory factory) && factory.CollectionFactory != null)
            {
                collectionFactory = factory.CollectionFactory;
            }
            // [TODO] Catch any exception and return an ResError
            return collectionFactory(cache.Client, rid);
        }

        private ResCollectionResource defaultCollectionFactory(ResClient client, string rid)
        {
            return new ResCollection(client, rid);
        }

        public object InitResource(ResResource resource, JToken data)
        {
            ResCollectionResource collection = resource as ResCollectionResource;
            if (collection == null)
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

            try
            {
                collection.Init(values);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }

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

        public void SynchronizeResource(string rid, object resource, JToken data, Action<ResourceEventArgs> onEvent)
        {
            var collection = (List<object>)resource;
            JArray arr = data as JArray;
            if (arr == null)
            {
                throw new InvalidOperationException("Collection data is not a json array.");
            }

            List<object> a = new List<object>(collection.Count);
            foreach (var value in collection)
            {
                a.Add(value);
            }

            List<object> b = new List<object>(arr.Count);
            foreach (var value in arr)
            {
                b.Add(cache.ParseValue(value, false));
            }

            patchDiff(
                a,
                b,
                (value, idx) => {
                    // Handle add event
                    collection.Insert(idx, value);
                    var resresource = value as ResResource;
                    if (resresource != null)
                    {
                        var ci = cache.GetItem(resresource.ResourceID);
                        ci.AddReference(1);
                    };
                    onEvent(new CollectionAddEventArgs
                    {
                        ResourceID = rid,
                        EventName = "add",
                        Index = idx,
                        Value = value,
                    });
                },
                (idx) => {
                    // Handle remove event
                    var value = collection[idx];
                    collection.RemoveAt(idx);

                    var resresource = value as ResResource;
                    if (resresource != null)
                    {
                        var ci = cache.GetItem(resresource.ResourceID);
                        ci.AddReference(-1);
                        cache.TryDelete(ci);
                    };

                    onEvent(new CollectionRemoveEventArgs
                    {
                        ResourceID = rid,
                        EventName = "remove",
                        Index = idx,
                        Value = value,
                    });
                }
            );
        }

        public void patchDiff(List<object> a, List<object> b, Action<object, int> onAdd, Action<int> onRemove)
        {

            // Do a LCS matric calculation
            // https://en.wikipedia.org/wiki/Longest_common_subsequence_problem
            //var t, i, j, s = 0, aa, bb, m = a.Count, n = b.Count;
            List<object> aa = a;
            List<object> bb = b;
            int i, j;
            int s = 0;
            int m = a.Count;
            int n = b.Count;

            // Trim of matches at the start and end
            while (s < m && s < n && Object.Equals(a[s], b[s]))
            {
                s++;
            }
            if (s == m && s == n)
            {
                return;
            }
            while (s < m && s < n && Object.Equals(a[m - 1], b[n - 1]))
            {
                m--;
                n--;
            }

            if (s > 0 || m < a.Count)
            {
                aa = a.GetRange(s, m - s);
                m = aa.Count;
            }
            if (s > 0 || n < b.Count)
            {
                bb = b.GetRange(s, n - s);
                n = bb.Count;
            }

            // Create matrix and initialize it
            var c = new int[m + 1, n + 1];

            for (i = 0; i < m; i++)
            {
                for (j = 0; j < n; j++)
                {
                    c[i + 1, j + 1] = Object.Equals(aa[i], bb[j])
                        ? c[i, j] + 1
                        : Math.Max(c[i + 1, j], c[i, j + 1]);
                }
            }

            int idx = m + s;
            i = m;
            j = n;
            int r = 0;
            var adds = new List<AddValue>();
            while (true)
            {
                m = i - 1;
                n = j - 1;
                if (i > 0 && j > 0 && Object.Equals(aa[m], bb[n]))
                {
                    idx--;
                    i--;
                    j--;
                }
                else if (j > 0 && (i == 0 || c[i, n] >= c[m, j]))
                {
                    adds.Add(new AddValue
                    {
                        ItemIdx = n,
                        IdxOffset = idx + r
                    });
                    j--;
                }
                else if (i > 0 && (j == 0 || c[i, n] < c[m, j]))
                {
                    onRemove(--idx);
                    r++;
                    i--;
                }
                else
                {
                    break;
                }
            }

            // Do the adds
            var len = adds.Count - 1;
            for (i = len; i >= 0; i--)
            {
                var add = adds[i];
                onAdd(bb[add.ItemIdx], add.IdxOffset - r  + len - i);
            }
        }


        public IEnumerable<object> GetResourceValues(object resource)
        {
            return (List<object>)resource;
        }
    }
}
