using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ItemCacheTests
    {
        public struct CacheEntry
        {
            public string RID;
            public int References;
            public int Subscriptions;
        }

        public struct ResourceSet
        {
            public string RID;
            public string[] Resources;
        }

        public readonly ITestOutputHelper Output;

        public ItemCacheTests(ITestOutputHelper output)
        {
            Output = output;
        }

        [Fact]
        public void Constructor_NoClient_CreatesItemCache()
        {
            var itemCache = new ItemCache();
            Assert.NotNull(itemCache);
        }

        public static IEnumerable<object[]> AddResourceAndSubscribe_PopulatesCache_Data => new List<object[]>
        {
            // Single
            new object[] {
                new ResourceSet
                {
                    RID = "model.a",
                    Resources = new string[] { "model.a" }
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 1, References = 0 },
                },
            },
            // Reference
            new object[] {
                new ResourceSet
                {
                    RID = "model.b-a",
                    Resources = new string[] { "model.b-a", "model.a" }
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.b-a", Subscriptions = 1, References = 0 },
                },
            },
            // Collection reference
            new object[] {
                new ResourceSet
                {
                    RID = "collection.g-a",
                    Resources = new string[] { "collection.g-a", "model.a" }
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "collection.g-a", Subscriptions = 1, References = 0 },
                },
            },
            // Complex reference
            new object[] {
                new ResourceSet
                {
                    RID = "model.c-ab",
                    Resources = new string[] { "model.c-ab", "model.b-a", "model.a" }
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 2 },
                    new CacheEntry { RID = "model.b-a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.c-ab", Subscriptions = 1, References = 0 },
                },
            },
            // Circular reference 
            new object[] {
                new ResourceSet
                {
                    RID = "model.d-e",
                    Resources = new string[] { "model.d-e", "model.e-d" }
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.d-e", Subscriptions = 1, References = 1 },
                    new CacheEntry { RID = "model.e-d", Subscriptions = 0, References = 1 },
                },
            },
            // Complex with circular reference
            new object[] {
                new ResourceSet
                {
                    RID = "model.f-bd",
                    Resources = new string[] { "model.f-bd", "model.d-e", "model.e-d", "model.b-a", "model.a" }
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.f-bd", Subscriptions = 1, References = 0 },
                    new CacheEntry { RID = "model.d-e", Subscriptions = 0, References = 2 },
                    new CacheEntry { RID = "model.e-d", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.b-a", Subscriptions = 0, References = 1 },
                },
            },
        };

        [Theory, MemberData(nameof(AddResourceAndSubscribe_PopulatesCache_Data))]
        public void AddResourcesAndSubscribe_PopulatesCache(ResourceSet resourceSet, CacheEntry[] expectedCacheEntries)
        {
            var itemCache = new ItemCache();
            itemCache.AddResourcesAndSubscribe(Test.ResourceSet(resourceSet.Resources), resourceSet.RID);

            var cache = itemCache.Cache;
            Assert.Equal(expectedCacheEntries.Length, cache.Count);
            foreach (var entry in expectedCacheEntries)
            {
                Assert.Contains(entry.RID, cache);
                Assert.Equal(entry.References, cache[entry.RID].References);
                Assert.Equal(entry.Subscriptions, cache[entry.RID].Subscriptions);
            }
        }



        public static IEnumerable<object[]> UnsubscribeResource_RemovesNonReferenced_Data => new List<object[]>
        {
            // Single unsubscribing all
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.a",
                        Resources = new string[] { "model.a" }
                    },
                },
                new CacheEntry[] { },
            },
            // Reference unsubscribing all
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.b-a",
                        Resources = new string[] { "model.b-a", "model.a" }
                    },
                },
                new CacheEntry[] { },
            },
            // Collection reference unsubscribing all
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "collection.g-a",
                        Resources = new string[] { "collection.g-a", "model.a" }
                    },
                },
                new CacheEntry[] { },
            },
            // Complex reference unsubscribing all
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.c-ab",
                        Resources = new string[] { "model.c-ab", "model.b-a", "model.a" }
                    },
                },
                new CacheEntry[] { },
            },
            // Circular reference unsubscribing all
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.d-e",
                        Resources = new string[] { "model.d-e", "model.e-d" }
                    },
                },
                new CacheEntry[] { },
            },
            // Complex with circular reference unsubscribing all
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.f-bd",
                        Resources = new string[] { "model.f-bd", "model.d-e", "model.e-d", "model.b-a", "model.a" }
                    },
                },
                new CacheEntry[] { },
            },

            // Reference unsubscribing subset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.a",
                        Resources = new string[] { "model.a" }
                    },
                    new ResourceSet
                    {
                        RID = "model.b-a",
                        Resources = new string[] { "model.b-a" }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.b-a", Subscriptions = 1, References = 0 },
                },
            },
            // Collection reference unsubscribing subset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.a",
                        Resources = new string[] { "model.a" }
                    },
                    new ResourceSet
                    {
                        RID = "collection.g-a",
                        Resources = new string[] { "collection.g-a" }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "collection.g-a", Subscriptions = 1, References = 0 },
                },
            },
            // Complex reference unsubscribing subset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.b-a",
                        Resources = new string[] { "model.b-a", "model.a" }
                    },
                    new ResourceSet
                    {
                        RID = "model.c-ab",
                        Resources = new string[] { "model.c-ab" }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 2 },
                    new CacheEntry { RID = "model.b-a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.c-ab", Subscriptions = 1, References = 0 },
                },
            },
            // Complex with circular reference unsubscribing subset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.d-e",
                        Resources = new string[] { "model.d-e", "model.e-d" }
                    },
                    new ResourceSet
                    {
                        RID = "model.f-bd",
                        Resources = new string[] { "model.f-bd", "model.b-a", "model.a" }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.f-bd", Subscriptions = 1, References = 0 },
                    new CacheEntry { RID = "model.d-e", Subscriptions = 0, References = 2 },
                    new CacheEntry { RID = "model.e-d", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.b-a", Subscriptions = 0, References = 1 },
                },
            },

            // Reference unsubscribing superset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.b-a",
                        Resources = new string[] { "model.b-a", "model.a" }
                    },
                    new ResourceSet
                    {
                        RID = "model.a",
                        Resources = new string[] { }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 1, References = 0 },
                },
            },
            // Collection reference unsubscribing superset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "collection.g-a",
                        Resources = new string[] { "collection.g-a", "model.a" }
                    },
                    new ResourceSet
                    {
                        RID = "model.a",
                        Resources = new string[] { }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 1, References = 0 },
                },
            },
            // Complex reference unsubscribing superset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.c-ab",
                        Resources = new string[] { "model.c-ab", "model.b-a", "model.a" }
                    },
                    new ResourceSet
                    {
                        RID = "model.b-a",
                        Resources = new string[] { }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.a", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.b-a", Subscriptions = 1, References = 0 },
                },
            },
            // Complex with circular reference unsubscribing superset
            new object[] {
                new ResourceSet[]
                {
                    new ResourceSet
                    {
                        RID = "model.f-bd",
                        Resources = new string[] { "model.f-bd", "model.b-a", "model.a", "model.d-e", "model.e-d" }
                    },
                    new ResourceSet
                    {
                        RID = "model.e-d",
                        Resources = new string[] { }
                    },
                },
                new CacheEntry[]
                {
                    new CacheEntry { RID = "model.d-e", Subscriptions = 0, References = 1 },
                    new CacheEntry { RID = "model.e-d", Subscriptions = 1, References = 1 },
                },
            },
        };

        [Theory, MemberData(nameof(UnsubscribeResource_RemovesNonReferenced_Data))]
        public void UnsubscribeResource_RemovesNonReferenced(ResourceSet[] resourceSets, CacheEntry[] expectedCacheEntries)
        {
            var itemCache = new ItemCache();
            foreach (var resourceSet in resourceSets)
            {
                itemCache.AddResourcesAndSubscribe(Test.ResourceSet(resourceSet.Resources), resourceSet.RID);
            }

            // Remove the subscribed resource
            var ci = itemCache.GetItem(resourceSets[0].RID);
            ci.AddSubscription(-1);
            itemCache.TryDelete(ci);

            var cache = itemCache.Cache;
            Assert.Equal(expectedCacheEntries.Length, cache.Count);
            foreach (var entry in expectedCacheEntries)
            {
                Assert.Contains(entry.RID, cache);
                Assert.Equal(entry.References, cache[entry.RID].References);
                Assert.Equal(entry.Subscriptions, cache[entry.RID].Subscriptions);
            }
        }
    }
}
