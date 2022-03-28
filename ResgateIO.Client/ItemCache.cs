using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResgateIO.Client
{
    internal class ItemCache
    {

        private Dictionary<string, CacheItem> itemCache = new Dictionary<string, CacheItem>();
        //private HashSet<string> stale = new HashSet<string>();
        private object cacheLock = new object();
        private IResourceType[] resourceTypes;
        public ResClient Client { get; private set; }

        public const int ResourceTypeModel = 0;
        public const int ResourceTypeCollection = 1;



        public ItemCache(ResClient client)
        {
            Client = client;
            createResourceTypes();
        }

        private void createResourceTypes()
        {
            resourceTypes = new IResourceType[]
            {
                 new ResourceTypeModel(this),
                 new ResourceTypeCollection(this)
            };
        }

        /// <summary>
        /// Registers a model factory for a specific resource pattern.
        /// The pattern may contain wildcards:
        /// * (asterisk) is a partial wildcard.
        ///     Eg. "directory.user.*.details"
        /// > (greater than) is a full wildcard.
        ///     Eg. "library.books.>"
        /// </summary>
        /// <param name="pattern">Resource name pattern.</param>
        /// <param name="factory">Model factory delegate.</param>
        public void RegisterModelFactory(string pattern, ModelFactory factory)
        {
            var rt = (ResourceTypeModel)resourceTypes[ResourceTypeModel];
            rt.Patterns.Add(pattern, factory);
        }

        /// <summary>
        /// Registers a collection factory for a specific resource pattern.
        /// The pattern may contain wildcards:
        /// * (asterisk) is a partial wildcard.
        ///     Eg. "directory.user.*.details"
        /// > (greater than) is a full wildcard.
        ///     Eg. "library.books.>"
        /// </summary>
        /// <param name="pattern">Resource name pattern.</param>
        /// <param name="factory">Collection factory delegate.</param>
        public void RegisterCollectionFactory(string pattern, CollectionFactory factory)
        {
            var rt = (ResourceTypeCollection)resourceTypes[ResourceTypeCollection];
            rt.Patterns.Add(pattern, factory);
        }

        public CacheItem GetOrSubscribe(string rid, Action<CacheItem> subscribe)
        {
            CacheItem ci;
            lock (cacheLock)
            {
                if (!itemCache.TryGetValue(rid, out ci))
                {
                    ci = new CacheItem(this, rid);
                    itemCache[rid] = ci;
                    subscribe(ci);
                }
            }

            return ci;
        }
        public CacheItem AddResourcesAndSubscribe(JToken result, string rid)
        {
            CacheItem ci;
            lock (cacheLock)
            {
                addResources(result);
                if (!itemCache.TryGetValue(rid, out ci))
                {
                    throw new ResException(String.Format("Resource not found in cache: {0}", rid));
                }
                ci.AddSubscription(1);
            }
            return ci;
        }

        public void TryDelete(CacheItem ci)
        {
            //throw new NotImplementedException();
        }

        public void AddResources(JToken data)
        {
            lock (cacheLock)
            {
                addResources(data);
            }
        }

        /// <summary>
        /// Gets a cache item from the cache, or throws an exception if it doesn't exist.
        /// </summary>
        /// <param name="rid">Resource ID</param>
        /// <returns>Cache item.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public CacheItem GetItem(string rid)
        {
            CacheItem ci;
            lock (cacheLock)
            {
                if (!itemCache.TryGetValue(rid, out ci))
                {
                    throw new InvalidOperationException(String.Format("Resource not found in cache: {0}", rid));
                }
            }
            return ci;
        }

        public ResourceEventArgs HandleEvent(ResourceEventArgs ev)
        {
            CacheItem ci = GetItem(ev.ResourceID);

            // Assert that the resource is set.
            // Should not be needed unless the gateway acts up.
            if (!ci.IsSet)
            {
                return null;
            }

            if (ev.EventName == "unsubscribe")
            {
                ev = handleUnsubscribeEvent(ci, ev);
            }
            else
            {
                var type = resourceTypes[(int)ci.Type];
                ev = type.HandleEvent(ci.InternalResource, ev);
            }

            if (ev != null)
            {
                try
                {
                    ci.Resource.HandleEvent(ev);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(String.Format("Exception on handling {0} event for resource {1}: {2}", ev.EventName, ev.ResourceID, ex.Message));
                }
            }
            return ev;
        }

        /// <summary>
        /// Adds a resources from a request result to the cache.
        /// The cacheLock must be held before call.
        /// </summary>
        /// <param name="result">Request result</param>
        private void addResources(JToken result)
        {
            if (result == null)
            {
                return;
            }

            JObject r = result as JObject;
            if (r == null)
            {
                return;
            }

            JObject[] typeResources = new JObject[resourceTypes.Length];
            Dictionary<string, JToken>[] typeSync = new Dictionary<string, JToken>[resourceTypes.Length];

            // Create empty resources for missing ones, and a dictionary of already existing resources to be synchronized.
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                IResourceType type = resourceTypes[i];
                JProperty resourceProp = r.Property(type.ResourceProperty);
                if (resourceProp != null)
                {
                    JObject resources = resourceProp.Value as JObject;
                    if (resources != null)
                    {
                        typeResources[i] = resources;
                        typeSync[i] = createResources(resources, type);
                    }
                }
            }

            // Initialize new resources with data
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                IResourceType type = resourceTypes[i];
                JObject resources = typeResources[i];
                if (resources != null)
                {
                    var sync = typeSync[i];
                    foreach (JProperty prop in resources.Properties())
                    {
                        string rid = prop.Name;
                        // Only initialize if not set for synchronization
                        if (sync == null || !sync.ContainsKey(rid))
                        {
                            CacheItem ci = itemCache[rid];
                            ci.SetInternalResource(type.InitResource(ci.Resource, prop.Value));

                        }
                    }
                }
            }

            // Synchronize stale resources with new data
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                var sync = typeSync[i];
                if (sync != null)
                {
                    IResourceType type = resourceTypes[i];
                    foreach (KeyValuePair<string, JToken> pair in sync)
                    {
                        type.SynchronizeResource(itemCache[pair.Key].Resource, pair.Value);
                    }
                }
            }

            // Complete all resource tasks
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                IResourceType type = resourceTypes[i];
                JObject resources = typeResources[i];
                if (resources != null)
                {
                    foreach (JProperty prop in resources.Properties())
                    {
                        string rid = prop.Name;
                        itemCache[rid].CompleteTask();
                    }
                }
            }
        }

        private Dictionary<string, JToken> createResources(JObject resources, IResourceType type)
        {
            Dictionary<string, JToken> sync = null;

            foreach (JProperty prop in resources.Properties())
            {
                string rid = prop.Name;
                CacheItem ci = null;
                if (!itemCache.TryGetValue(rid, out ci))
                {
                    // If the resource is not cached since before, create a new cache item for it.
                    ci = new CacheItem(this, rid);
                    itemCache[rid] = ci;
                }
                else
                {
                    // If the resource was cached, it might have been stale.
                    // removeStale(rid)
                }

                // If it is set since before, it is stale and needs to be updated
                if (ci.IsSet)
                {
                    if (ci.Resource.Type != type.ResourceType)
                    {
                        throw new InvalidOperationException("Resource type inconsistency");
                    }

                    sync = sync ?? new Dictionary<string, JToken>();
                    sync[rid] = prop.Value;
                }
                else
                {
                    ci.SetResource(type.CreateResource(rid), type.ResourceType);
                }
            }

            return sync;
        }

        public object ParseValue(JToken value, bool addIndirect)
        {
            if (value.Type == JTokenType.Null)
            {
                return null;
            }

            var obj = value as JObject;
            if (obj != null)
            {
                // Test for resource reference
                JToken ridToken = obj["rid"];
                if (ridToken != null)
                {
                    var rid = ridToken.Value<string>();

                    // Test for soft reference
                    JToken softToken = obj["soft"];
                    if (softToken != null && softToken.Value<bool>())
                    {
                        return new ResRef(rid);
                    }

                    CacheItem item = itemCache[rid];
                    if (addIndirect)
                    {
                        item.AddReference(1);
                    }
                    return item.Resource;
                }

                // Test for data value
                JToken dataToken = obj["data"];
                if (dataToken != null)
                {
                    return dataToken;
                }

                // Test for action value
                JToken actionToken = obj["action"];
                if (actionToken != null)
                {
                    if (actionToken.Value<string>() == "delete")
                    {
                        return ResAction.Delete;
                    }
                }
            }
            else
            {
                var val = value as JValue;
                if (val != null)
                {
                    return val.Value;
                }
            }

            throw new InvalidOperationException("Invalid RES value: " + value.ToString(Formatting.None));
        }

        private ResourceEventArgs handleUnsubscribeEvent(CacheItem ci, ResourceEventArgs ev)
        {
            ci.ClearSubscriptions();
            TryDelete(ci);

            ResError reason = null;
            JObject data = ev.Data as JObject;
            if (data != null)
            {
                JToken reasonToken = data["reason"];
                if (reasonToken != null)
                {
                    reason = reasonToken.ToObject<ResError>();
                }
            }

            if (reason == null) {
                reason = new ResError("Missing unsubscribe reason.");
            }

            return new ResourceUnsubscribeEventArgs
            {
                ResourceID = ev.ResourceID,
                EventName = ev.EventName,
                Data = ev.Data,
                Reason = reason,
            };
        }

    }
}
