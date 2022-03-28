using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
    public class ResourceEventArgs : EventArgs
    {
        public string ResourceID { get; set; }
        public string EventName { get; set; }
        public JToken Data { get; set; }
    }

    public class ResourceUnsubscribeEventArgs : ResourceEventArgs
    {
        public ResError Reason { get; set; }
    }

    public class ModelChangeEventArgs : ResourceEventArgs
    {
        public IReadOnlyDictionary<string, object> NewValues { get; set; }
        public IReadOnlyDictionary<string, object> OldValues { get; set; }
    }

    public class CollectionAddEventArgs : ResourceEventArgs
    {
        public int Index { get; set; }
        public object Value { get; set; }
    }

    public class CollectionRemoveEventArgs : ResourceEventArgs
    {
        public int Index { get; set; }
        public object Value { get; set; }
    }
}
