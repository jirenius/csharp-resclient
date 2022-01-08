using System;
using Newtonsoft.Json.Linq;

namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a primitive value such as a string, number, or boolean.
    /// </summary>
    public class ResPrimitive : ResValue, IEquatable<ResPrimitive>
    {
        public override object Value
        {
            get
            {
                return value.Value;
            }
        }

        private JValue value;

        /// <summary>
        /// Initializes a new instance of the ResPrimitive class.
        /// </summary>
        public ResPrimitive(JValue value)
        {
            this.value = value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(ResValue other)
        {
            return value.Equals(other as ResPrimitive);
        }

        public bool Equals(ResPrimitive other)
        {
            return value.Equals(other.Value);
        }
    }
}