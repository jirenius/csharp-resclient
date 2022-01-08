using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
    class ResourceTypeModel : IResourceType
    {
        public ResourceType ResourceType { get { return ResourceType.Model; } }

        public string ResourceProperty { get { return "models"; } }

        private ResClient client;

        public ResourceTypeModel(ResClient client)
        {
            this.client = client;
        }

        public ResResource CreateResource(string rid)
        {
            return new ResModel(rid);
        }

        public void InitResource(ResResource resource, JToken data)
        {
            ResModel model = resource as ResModel;
            if (model == null)
            {
                throw new InvalidOperationException("Resource not of type ResModel.");
            }

            JObject obj = data as JObject;
            if (obj == null)
            {
                throw new InvalidOperationException("Model data is not a json object.");
            }

            var props = new Dictionary<string, object>(obj.Count);
            foreach (JProperty prop in obj.Properties())
            {
                props[prop.Name] = client.ParseValue(prop.Value, true);
            }

            model.Init(props);
        }
    }
}
