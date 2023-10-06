namespace ResgateIO.Client
{
    internal readonly struct ResourceFactory
    {
        public readonly CollectionFactory CollectionFactory;
        public readonly ModelFactory ModelFactory;

        public ResourceFactory(ModelFactory factory)
        {
            ModelFactory = factory;
            CollectionFactory = null;
        }

        public ResourceFactory(CollectionFactory factory)
        {
            CollectionFactory = factory;
            ModelFactory = null;
        }
    }
}
