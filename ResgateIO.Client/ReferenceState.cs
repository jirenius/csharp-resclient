using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
    internal enum ReferenceState
    {
        None,
        Delete,
        Keep,
        Stale,
        Stop
    }

    internal class CacheItemReference
    {
        public CacheItem Item { get; set; }
        public int Count { get; set; }
        public ReferenceState State { get; set; }
    }
}
