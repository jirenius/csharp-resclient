using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ResClientExamples : TestsBase
    {
        public ResClientExamples(ITestOutputHelper output) : base(output) { }

        [Fact(Skip = "example code")]
        public async Task ExampleAsync_GettingDefaultResources()
        {
            // Creating a client using a hostUrl string
            var client = new ResClient("ws://127.0.0.1:8080");

            // Getting a model of the default type ResModel
            var model = await client.GetAsync("example.model") as ResModel;

            // Accessing a model value by property
            Console.WriteLine("Model property foo: {0}", model["foo"]);

            // Getting a collection of the default type ResCollection
            var collection = await client.GetAsync("example.collection") as ResCollection;

            // Accessing a collection value by index
            Console.WriteLine("Collection value at index 0: {0}", collection[0]);
        }

        /// <summary>
        /// User represents a user resource model.
        /// </summary>
        public class User : ResModelResource
        {
            public string Name;
            public string Surname;

            public User(string rid) : base(rid) { }

            public override void Init(IReadOnlyDictionary<string, object> props)
            {
                Name = (props["name"] as JObject).Value<string>();
                Surname = (props["name"] as JObject).Value<string>();
            }

            public override void HandleChange(IReadOnlyDictionary<string, object> props)
            {
                if (props.TryGetValue("name", out object name))
                {
                    Name = (name as JObject).Value<string>();
                }
                if (props.TryGetValue("surname", out object surname))
                {
                    Surname = (surname as JObject).Value<string>();
                }
            }
        }

        [Fact(Skip = "example code")]
        public async Task ExampleAsync_GettingCustomDefinedResources()
        {
            // Creating a client using a hostUrl string
            var client = new ResClient("ws://127.0.0.1:8080");

            // Registering user model factory
            client.RegisterModelFactory("example.user.*", (client, rid) => new User(rid));
            client.RegisterCollectionFactory("example.users", (client, rid) => new ResCollection<User>(rid));

            // Getting a model of the default type ResModel
            var users = await client.GetAsync("example.users") as ResCollection<User>;

            // Iterate over all users
            foreach (User user in users)
            {
                Console.WriteLine("User: {0} {1}", user.Name, user.Surname);
            }
        }


        [Fact(Skip = "example code")]
        public async Task ExampleAsync_ListeningToEvent()
        {
            // Creating a client using a hostUrl string
            var client = new ResClient("ws://127.0.0.1:8080");

            // Listening for any resource event
            client.ResourceEvent += client_ResourceEvent;

            // Getting a model of the default type ResModel
            var model = await client.GetAsync("example.model") as ResModel;

            // Listening for model change
            model.ChangeEvent += model_ChangeEvent;
            await Task.Delay(1000);
            // Stop listening for model change
            model.ChangeEvent -= model_ChangeEvent;
        }

        private void model_ChangeEvent(object sender, ChangeEventArgs e)
        {
            Console.WriteLine("Model change");
        }

        private void client_ResourceEvent(object sender, ResourceEventArgs e)
        {
            Console.WriteLine("Event for resource {0}: {1}", e.ResourceID, e.EventName);
        }

        /// <summary>
        /// Book represents a book resource model.
        /// </summary>
        public class Book : ResModelResource
        {
            public string Title { get; private set; }
            public string Author { get; private set; }

            public readonly ResClient Client;

            public Book(ResClient client, string rid) : base(rid) {
                Client = client;
            }

            public override void Init(IReadOnlyDictionary<string, object> props)
            {
                Title = (props["title"] as JObject).Value<string>();
                Author = (props["author"] as JObject).Value<string>();
            }

            public override void HandleChange(IReadOnlyDictionary<string, object> props)
            {
                if (props.TryGetValue("title", out object name))
                {
                    Title = (name as JObject).Value<string>();
                }
                if (props.TryGetValue("author", out object surname))
                {
                    Author = (surname as JObject).Value<string>();
                }
            }
        }

        [Fact(Skip = "example code")]
        public async Task ExampleAsync_CallMethodOnModel()
        {
            // Creating a client using a hostUrl string
            var client = new ResClient("ws://127.0.0.1:8080");

            // Registering user model factory
            client.RegisterModelFactory("example.user.*", (client, rid) => new User(rid));
            client.RegisterCollectionFactory("example.users", (client, rid) => new ResCollection<User>(rid));

            // Getting a model of the default type ResModel
            var users = await client.GetAsync("example.users") as ResCollection<User>;

            // Iterate over all users
            foreach (User user in users)
            {
                Console.WriteLine("User: {0} {1}", user.Name, user.Surname);
            }
        }
    }
}
