namespace ResgateIO.Client
{
    /// <summary>
    /// Represents a RES soft reference.
    /// </summary>
    public class ResRef
    {
        public string ResourceID { get; }

        /// <summary>
        /// Initializes a new instance of the ResRef class.
        /// </summary>
        public ResRef(string rid)
        {
            ResourceID = rid;
        }
    }
}