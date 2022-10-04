using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ResgateIO.Client
{
    class CacheItem
    {
        public ResResource Resource { get; private set; }
        public string ResourceID { get { return rid; } }
        public ResourceType Type { get; private set; }
        public bool IsSet { get { return completionSource.Task.IsCompleted; } }
        public Task<ResResource> ResourceTask { get { return completionSource.Task; } }
        public int Subscriptions { get { return subscriptions; } }
        public int VirtualSubscriptions { get { return virtualSubscriptions; } }
        public int References { get { return references; } }
        public bool IsStale { get { return subscriptions == 0 && virtualSubscriptions > 0; } }

        // Internal resources
        public object InternalResource { get; private set; }

        private TaskCompletionSource<ResResource> completionSource = new TaskCompletionSource<ResResource>();

        private readonly string rid;
        private readonly ItemCache cache;
        private int references = 0; // Indirect references by other resources
        private int subscriptions = 0; // Direct references through subscriptions
        private int virtualSubscriptions = 0; // Direct references through subscriptions not propagated to the gateway

        public CacheItem(ItemCache cache, string rid)
        {
            this.cache = cache;
            this.rid = rid;

            //var r = new WeakReference(new List<string>(), true);
        }

        /// <summary>
        /// Increases the subscription count with one.
        /// </summary>
        /// <param name="allowVirtual">If true, the virtualSubscription counter will be increased if subscriptions > 0</param>
        /// <returns>Returns true if subscriptions were increased, or false if virtualSubscriptions were increased.</returns>
        public bool AddSubscription(bool allowVirtual)
        {
            if (allowVirtual && subscriptions > 0)
            {
                virtualSubscriptions++;
                return false;
            }
            subscriptions++;
            return true;
        }

        /// <summary>
        /// Increases the subscription count with one.
        /// </summary>
        /// <param name="allowVirtual">If true, the virtualSubscription counter will be decreased if virtualSubscriptions > 0</param>
        /// <returns>Returns true if subscriptions were decreased, or false if virtualSubscriptions were decreased.</returns>
        public bool RemoveSubscription(bool allowVirtual)
        {
            if (allowVirtual && virtualSubscriptions > 0)
            {
                virtualSubscriptions--;
                return false;
            }
            subscriptions--;
            if (subscriptions == 0)
            {
                virtualSubscriptions = 0;
            }
            return true;
        }

        public void ClearSubscriptions()
        {
            subscriptions = 0;
            virtualSubscriptions = 0;
        }

        public void AddReference(int delta)
        {
            this.references += delta;
            if (this.references < 0)
            {
                throw new InvalidOperationException("Indirect reference count reached below 0 for resource: " + this.rid);
            }
        }

        public void TrySetException(Exception ex)
        {
            completionSource.TrySetException(ex);
        }

        public void SetStale()
        {
            virtualSubscriptions += subscriptions;
            subscriptions = 0;
        }

        /// <summary>
        /// Tries to decrease the virtualSubscription counter with 1 and increase the subscription counter with 1.
        /// </summary>
        /// <returns>True if virtualSubscription counter were greater than 0 and subscription counter were 0.</returns>
        public bool TrySubscribeStale()
        {
            if (virtualSubscriptions > 0 && subscriptions == 0)
            {
                virtualSubscriptions--;
                subscriptions++;
                return true;
            }
            return false;
        }

        public void SetResource(ResResource resource, ResourceType resourceType)
        {
            Resource = resource;
            Type = resourceType;
        }

        public void SetInternalResource(object resource)
        {
            InternalResource = resource;
        }

        public void CompleteTask()
        {
            completionSource.TrySetResult(Resource);
        }
    }
}
