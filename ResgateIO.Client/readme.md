# RES Client for .NET : Synchronize Your Clients

.NET client library implementing the RES-Client Protocol. Used to establish WebSocket connections to [Resgate](https://github.com/resgateio/resgate).

Visit [Resgate.io](https://resgate.io) for more information.  
Visit [the GitHub repository](https://github.com/jirenius/csharp-resclient) for the source code.

## Basic usage

```csharp
// Creating a client using a hostUrl string.
var client = new ResClient("ws://127.0.0.1:8080");

// Subscribing to a model of the default type ResModel.
var model = await client.SubscribeAsync("example.model") as ResModel;

// Accessing a model value by property.
Console.WriteLine("Model property foo: {0}", model["foo"]);

// Unsubscribing to a model (same as collection).
await client.UnsubscribeAsync("example.model");

// Getting a collection of the default type ResCollection.
var collection = await client.SubscribeAsync("example.collection") as ResCollection;

// Accessing a collection value by index.
Console.WriteLine("Collection value at index 0: {0}", collection[0]);

// Unsubscribing to a collection (same as model).
await client.UnsubscribeAsync("example.collection");
```

### Defining a custom model class

```csharp
/// <summary>
/// Mail represents a mail message resource model.
/// Used in the basic examples below.
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
		Subject = props["subject"] as string;
		Sender = props["sender"] as string;
		Body = props["body"] as string;
	}

	public override void HandleEvent(ResourceEventArgs ev)
	{
		switch (ev)
		{
			case ModelChangeEventArgs changeEv:
				if (changeEv.NewValues.TryGetValue("subject", out object subject))
				{
					Subject = subject as string;
				}
				if (changeEv.NewValues.TryGetValue("sender", out object sender))
				{
					Sender = sender as string;
				}
				if (changeEv.NewValues.TryGetValue("body", out object body))
				{
					Body = body as string;
				}
				break;
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
```

### Using custom resource factories

```csharp
// Creating a client using a hostUrl string.
var client = new ResClient("ws://127.0.0.1:8080");

// Registering mail model and collection factories for the patterns.
client.RegisterModelFactory("example.mail.*", (client, rid) => new Mail(client, rid));
client.RegisterCollectionFactory("example.mails", (client, rid) => new ResCollection<Mail>(client, rid));

// Getting a collection of registered types.
var mails = await client.SubscribeAsync("example.mails") as ResCollection<Mail>;

// Iterate over all mails
foreach (Mail mail in mails)
{
	Console.WriteLine("Mail: {0} {1}", mail.Subject, mail.Body);
}
```

### Calling methods
```csharp
// Creating a client using a hostUrl string
var client = new ResClient("ws://127.0.0.1:8080");

// Registering mail model factory
client.RegisterModelFactory("example.mail.*", (client, rid) => new Mail(client, rid));

// Calling a method assuming it returns a mail model in a resource response.
var mail = await client.CallAsync("example.mails", "getLastMail") as Mail;
var totalMails = await client.CallAsync<int>("example.mails", "getTotalMails");

// Call model method
await mail.ReplyAsync("This is my reply");
```

### Listening to events

```csharp
// Creating a client using a hostUrl string
var client = new ResClient("ws://127.0.0.1:8080");

// Listening for any resource event
client.ResourceEvent += (sender, e) =>
{
	Console.WriteLine("Event for resource {0}: {1}", e.ResourceID, e.EventName);
};

// Getting a model of the default type ResModel
var model = await client.SubscribeAsync("example.model") as ResModel;

// Listening for model change
EventHandler<ResourceEventArgs> handler = (sender, e) =>
{
	switch (e)
	{
		case ModelChangeEventArgs changeEvent:
			Console.WriteLine("Model change event");
			break;
		default:
			Console.WriteLine("Custom event: ", e.EventName);
			break;
	}
};
model.ResourceEvent += handler;
await Task.Delay(1000);
// Stop listening for model change
model.ResourceEvent -= handler;
```


## Contributing

If you find any issues, feel free to [report them](https://github.com/jirenius/csharp-resclient/issues/new) as an Issue.
