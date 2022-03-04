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
        /// Mail represents a mail message resource model.
        /// </summary>
        public class Mail : ResModelResource
        {
            public string Subject { get; private set; }
            public string Sender { get; private set; }
            public string Body { get; private set; }

            public readonly ResClient Client;

            public Mail(ResClient client, string rid) : base(rid)
            {
                Client = client;
            }

            public override void Init(IReadOnlyDictionary<string, object> props)
            {
                Subject = (props["subject"] as JObject).Value<string>();
                Sender = (props["sender"] as JObject).Value<string>();
                Body = (props["body"] as JObject).Value<string>();
            }

            public override void HandleChange(IReadOnlyDictionary<string, object> props)
            {
                if (props.TryGetValue("subject", out object subject))
                {
                    Subject = (subject as JObject).Value<string>();
                }
                if (props.TryGetValue("sender", out object sender))
                {
                    Sender = (sender as JObject).Value<string>();
                }
                if (props.TryGetValue("body", out object body))
                {
                    Body = (body as JObject).Value<string>();
                }
            }

            public async Task ReplyAsync(string message)
            {
                await Client.CallAsync(ResourceID, "reply", new
                {
                    message = message
                });
            }
        }

        [Fact(Skip = "example code")]
        public async Task ExampleAsync_GettingCustomDefinedResources()
        {
            // Creating a client using a hostUrl string
            var client = new ResClient("ws://127.0.0.1:8080");

            // Registering mail model and collection factories
            client.RegisterModelFactory("example.mail.*", (client, rid) => new Mail(client, rid));
            client.RegisterCollectionFactory("example.mails", (client, rid) => new ResCollection<Mail>(client, rid));

            // Getting a collection of registered types.
            var mails = await client.GetAsync("example.mails") as ResCollection<Mail>;

            // Iterate over all mails
            foreach (Mail mail in mails)
            {
                Console.WriteLine("Mail: {0} {1}", mail.Subject, mail.Body);
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

        [Fact(Skip = "example code")]
        public async Task ExampleAsync_CallMethods()
        {
            // Creating a client using a hostUrl string
            var client = new ResClient("ws://127.0.0.1:8080");

            // Registering mail model factory
            client.RegisterModelFactory("example.mail.*", (client, rid) => new Mail(client, rid));

            // Calling a method assuming it returns a mail model in a resource response.
            var mail = await client.CallAsync("example.mails", "getLastMail") as Mail;
            var totalMails = await client.CallAsync<int>("example.mails", "getTotalMails");            

            // Call model method
            await mail.ReplyAsync("This is my reply");
        }
    }
}
