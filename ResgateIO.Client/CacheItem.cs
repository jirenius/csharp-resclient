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

        // Internal resources
        public object InternalResource { get; private set; }

        private TaskCompletionSource<ResResource> completionSource = new TaskCompletionSource<ResResource>();

        private readonly string rid;
        private readonly ItemCache cache;
        private int directReferences = 0;
        private int references = 0; // Indirect references by other resources
        private int subscriptions = 0; // Direct references through subscriptions

        public CacheItem(ItemCache cache, string rid)
        {
            this.cache = cache;
            this.rid = rid;

            //var r = new WeakReference(new List<string>(), true);
        }

        public void AddSubscription(int delta)
        {
            subscriptions += delta;
            //if (subscriptions == 0 && this.unsubTimeout)
            //{
            //    clearTimeout(this.unsubTimeout);
            //    this.unsubTimeout = null;
            //}
        }

        public void ClearSubscriptions()
        {
            AddSubscription(-subscriptions);
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

        public void ClearSubscribed()
        {

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
