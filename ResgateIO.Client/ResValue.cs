using System;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES value.
    /// </summary>
    public abstract class ResValue : IEquatable<ResValue>
    {
        /// <summary>
        /// Gets the underlying token value.
        /// </summary>
        public abstract object Value { get; }

        public abstract override int GetHashCode();

        public abstract bool Equals(ResValue other);

        public override bool Equals(object obj)
        {
            return Equals(obj as ResValue);
        }
    }
}