using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client.UnitTests
{ 

    /// <summary>
    /// MockModel represents a custom resource model.
    /// </summary>
    public class MockModel : ResModelResource
    {
        public string String { get; private set; }
        public int Int { get; private set; }

        /// <summary>
        /// Exception to throw when the Init method is called.
        /// </summary>
        public Exception InitException { get; set; }
        /// <summary>
        /// Exception to throw when the HandleEvent method is called.
        /// </summary>
        public Exception HandleEventException { get; set; }

        public readonly ResClient Client;

        public MockModel(ResClient client, string rid) : base(rid)
        {
            Client = client;
        }

        public override void Init(IReadOnlyDictionary<string, object> props)
        {
            if (InitException != null)
            {
                throw InitException;
            }

            String = props["string"] as string;
            Int = Convert.ToInt32(props["int"]);
        }

        public override void HandleEvent(ResourceEventArgs ev)
        {
            if (HandleEventException != null)
            {
                throw HandleEventException;
            }

            switch (ev)
            {
                case ModelChangeEventArgs changeEv:
                    if (changeEv.NewValues.TryGetValue("string", out object stringValue))
                    {
                        String = stringValue as string;
                    }
                    if (changeEv.NewValues.TryGetValue("int", out object intValue))
                    {
                        Int = Convert.ToInt32(intValue);
                    }
                    break;
            }
        }
    }
}
