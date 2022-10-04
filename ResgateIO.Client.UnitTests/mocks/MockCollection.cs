using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client.UnitTests
{


    public class MockCollection : ResCollection<object>
    {
        /// <summary>
        /// Exception to throw when the Init method is called.
        /// </summary>
        public Exception InitException { get; set; }
        /// <summary>
        /// Exception to throw when the HandleEvent method is called.
        /// </summary>
        public Exception HandleEventException { get; set; }

        public MockCollection(ResClient client, string rid) : base(client, rid) { }

        /// <summary>
        /// Initializes the collection with values.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="values">Collection values.</param>
        public override void Init(IReadOnlyList<object> values)
        {
            if (InitException != null)
            {
                throw InitException;
            }

            base.Init(values);
        }

        /// <summary>
        /// Handles incoming events.
        /// </summary>
        /// <remarks>Not to be called directly. Used by ResClient.</remarks>
        /// <param name="ev">Resource event.</param>
        public override void HandleEvent(ResourceEventArgs ev)
        {
            if (HandleEventException != null)
            {
                throw HandleEventException;
            }

            base.HandleEvent(ev);
        }
    }
}
