using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ResCollectionTests : TestsBase
    {
        public ResCollectionTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("\"foo\"", "foo")]
        [InlineData("null", null)]
        [InlineData("42", 42)]
        [InlineData("true", true)]
        [InlineData("[\"foo\",null,42,true]", new object[] { "foo", null, 42, true })]
        public async Task CallAsync_AnonymousResult_GetsResult(string payload, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.SubscribeAsync("test.collection");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.collection");
            req1.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", Test.Collection }
            } } });
            var collection1 = await creqTask1 as ResCollection;

            var creqTask2 = collection1.CallAsync("method");
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("call.test.collection.method");
            req2.SendResult(new JObject { { "payload", JToken.Parse(payload) } });
            var result = await creqTask2;
            Test.AssertEqualJSON(expected == null ? JValue.CreateNull() : JToken.FromObject(expected), result);
        }

        [Theory]
        [InlineData("\"foo\"", "foo")]
        [InlineData("null", null)]
        [InlineData("42", 42)]
        [InlineData("true", true)]
        [InlineData("[\"foo\",null,42,true]", new object[] { "foo", null, 42, true })]
        public async Task AuthAsync_AnonymousResult_GetsResult(string payload, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.SubscribeAsync("test.collection");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.collection");
            req1.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", Test.Collection }
            } } });
            var collection1 = await creqTask1 as ResCollection;

            var creqTask2 = collection1.AuthAsync("method");
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("auth.test.collection.method");
            req2.SendResult(new JObject { { "payload", JToken.Parse(payload) } });
            var result = await creqTask2;
            Test.AssertEqualJSON(expected == null ? JValue.CreateNull() : JToken.FromObject(expected), result);
        }

        [Fact]
        public async Task SubscribeAsync_WithCustomCollectionFactory_GetsCustomCollectionl()
        {
            Client.RegisterModelFactory("test.custom.*", (client, rid) => new MockModel(client, rid));
            Client.RegisterCollectionFactory("test.custom", (client, rid) => new ResCollection<MockModel>(client, rid));
            await ConnectAndHandshake();

            var creqTask = Client.SubscribeAsync("test.custom");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.custom");
            req.SendResult(new JObject
            {
                { "models", new JObject
                    {
                        { "test.custom.42", Test.CustomModelData }
                    }
                },
                { "collections", new JObject
                    {
                        { "test.custom", new JArray { new JObject { { "rid", "test.custom.42" } } } }
                    }
                }
            });
            var result = await creqTask;
            Assert.Equal("test.custom", result.ResourceID);
            Assert.IsType<ResCollection<MockModel>>(result);
            var collection = result as ResCollection<MockModel>;
            Assert.Single(collection);
            Assert.IsType<MockModel>(collection[0]);
            var model = collection[0];
            Assert.Equal("test.custom.42", model.ResourceID);
            Assert.Equal("foo", model.String);
            Assert.Equal(42, model.Int);
        }

        public static IEnumerable<object[]> AddEvent_PrimitiveValue_UpdatesCollection_Data => new List<object[]>
        {
            // Change single value
            new object[] { 0, "D", new JArray { "D", "A", "B", "C" } },
            new object[] { 1, "D", new JArray { "A", "D", "B", "C" } },
            new object[] { 2, "D", new JArray { "A", "B", "D", "C" } },
            new object[] { 3, "D", new JArray { "A", "B", "C", "D" } },
        };

        [Theory, MemberData(nameof(AddEvent_PrimitiveValue_UpdatesCollection_Data))]
        public async Task AddEvent_PrimitiveValue_UpdatesCollection(int addIndex, string addValue, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.SubscribeAsync("test.collection");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.collection");
            req1.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", new JArray { "A", "B", "C" } }
            } } });
            var collection1 = await creqTask1 as ResCollection;

            var completionSource = new TaskCompletionSource<ResourceEventArgs>();
            EventHandler<ResourceEventArgs> h = (object sender, ResourceEventArgs e) => completionSource.SetResult(e);
            collection1.ResourceEvent += h;

            byte[] eventMsg = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.collection.add\",\"data\":{\"idx\":" + addIndex.ToString() + ",\"value\":\"" + addValue + "\"}}");
            WebSocket.SendMessage(eventMsg);

            var ev = await completionSource.Task;

            collection1.ResourceEvent -= h;

            Assert.IsType<CollectionAddEventArgs>(ev);
            var addEv = (CollectionAddEventArgs)ev;

            Assert.Equal(addIndex, addEv.Index);
            Assert.Equal(addValue, addEv.Value);
            Test.AssertEqualJSON(expected, collection1);
        }



        public static IEnumerable<object[]> RemoveEvent_PrimitiveValue_UpdatesCollection_Data => new List<object[]>
        {
            // Change single value
            new object[] { 0, "A", new JArray { "B", "C", "D" } },
            new object[] { 1, "B", new JArray { "A", "C", "D" } },
            new object[] { 2, "C", new JArray { "A", "B", "D" } },
            new object[] { 3, "D", new JArray { "A", "B", "C" } },
        };

        [Theory, MemberData(nameof(RemoveEvent_PrimitiveValue_UpdatesCollection_Data))]
        public async Task RemoveEvent_PrimitiveValue_UpdatesCollection(int addIndex, string expectedRemoveValue, object expected)
        {
            await ConnectAndHandshake();
            var creqTask1 = Client.SubscribeAsync("test.collection");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.collection");
            req1.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", new JArray { "A", "B", "C", "D" } }
            } } });
            var collection1 = await creqTask1 as ResCollection;

            var completionSource = new TaskCompletionSource<ResourceEventArgs>();
            EventHandler<ResourceEventArgs> h = (object sender, ResourceEventArgs e) => completionSource.SetResult(e);
            collection1.ResourceEvent += h;

            byte[] eventMsg = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.collection.remove\",\"data\":{\"idx\":" + addIndex.ToString() + "}}");
            WebSocket.SendMessage(eventMsg);

            var ev = await completionSource.Task;

            collection1.ResourceEvent -= h;

            Assert.IsType<CollectionRemoveEventArgs>(ev);
            var removeEv = (CollectionRemoveEventArgs)ev;

            Assert.Equal(addIndex, removeEv.Index);
            Assert.Equal(expectedRemoveValue, removeEv.Value);
            Test.AssertEqualJSON(expected, collection1);
        }

        [Fact]
        public async Task SubscribeAsync_WithExceptionInInitMethod_RaisesError()
        {
            var ex = new Exception("Exception thrown.");

            Client.RegisterCollectionFactory("test.collection", (client, rid) =>
            {
                var collection = new MockCollection(client, rid);
                collection.InitException = ex;
                return collection;
            });
            await ConnectAndHandshake();

            var creqTask = Client.SubscribeAsync("test.collection");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.collection");
            req.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", Test.Collection }
            } } });

            var result = await creqTask;
            Assert.Equal("test.collection", result.ResourceID);
            Assert.IsType<MockCollection>(result);

            var ev = await NextError();
            Assert.Equal(ex, ev.GetException());
        }

        [Fact]
        public async Task SubscribeAsync_WithExceptionInEventHandler_RaisesError()
        {
            var ex = new Exception("Exception thrown.");

            Client.RegisterCollectionFactory("test.collection", (client, rid) =>
            {
                var collection = new MockCollection(client, rid);
                collection.HandleEventException = ex;
                return collection;
            });
            await ConnectAndHandshake();

            var creqTask1 = Client.SubscribeAsync("test.collection");
            var req1 = await WebSocket.GetRequestAsync();
            req1.AssertMethod("subscribe.test.collection");
            req1.SendResult(new JObject { { "collections", new JObject {
                { "test.collection", Test.Collection }
            } } });
            var collection1 = await creqTask1 as MockCollection;

            byte[] eventMsg = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.collection.add\",\"data\":{\"idx\":2,\"value\":\"baz\"}}");
            WebSocket.SendMessage(eventMsg);

            var ev = await NextError();
            Assert.Equal(ex, ev.GetException());
        }

        public static IEnumerable<object[]> Synchronize_WithPrimitiveCollection_EmitsExpectedEvents_Data => new List<object[]>
        {
            // Add single value
            new object[] { new JArray { "A", "B" }, new JArray { "C", "A", "B" }, 1},
        };

        [Theory, MemberData(nameof(Synchronize_WithPrimitiveCollection_EmitsExpectedEvents_Data))]
        public async Task Synchronize_WithPrimitiveCollection_EmitsExpectedEvents(JArray initCollection, JArray resyncCollection, int expectedEvents)
        {
            await ConnectAndHandshake();

            var creqTask = Client.SubscribeAsync("test.collection");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.collection");
            req.SendResult(new JObject
            {
                { "collections", new JObject
                    {
                        { "test.collection", initCollection }
                    }
                }
            });
            var collection = await creqTask as ResCollection;
            // Clone the collection to later verify the events produce the same endresult
            var clone = new List<object>(collection.Count);
            foreach (var item in collection)
            {
                clone.Add(item);
            }

            // Disconnect and reconnect
            await WebSocket.DisconnectAsync();
            await ConnectAndHandshake();

            // Expect resynchronization get request and generated change event
            var completionSource = new TaskCompletionSource<ResourceEventArgs>();
            EventHandler<ResourceEventArgs> h = (object sender, ResourceEventArgs e) => completionSource.SetResult(e);
            collection.ResourceEvent += h;
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("subscribe.test.collection");
            req2.SendResult(new JObject
            {
                { "collections", new JObject
                    {
                        { "test.collection", resyncCollection }
                    }
                }
            });

            // Follow resync with custom event to flush the events
            byte[] eventMsg = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.collection.custom\",\"data\":null}");
            WebSocket.SendMessage(eventMsg);

            List<ResourceEventArgs> events = new List<ResourceEventArgs>();
            while (true)
            {
                var ev = await completionSource.Task;
                if (ev.EventName == "custom")
                {
                    break;
                }
                events.Add(ev);
            }
            collection.ResourceEvent -= h;

            Assert.Equal(expectedEvents, events.Count);

            foreach (var ev in events)
            {
                switch (ev.EventName)
                {
                    case "add":
                        Assert.IsType<CollectionAddEventArgs>(ev);
                        var addEv = (CollectionAddEventArgs)ev;
                        clone.Insert(addEv.Index, addEv.Value);
                        break;
                    case "remove":
                        Assert.IsType<CollectionRemoveEventArgs>(ev);
                        var removeEv = (CollectionRemoveEventArgs)ev;
                        clone.RemoveAt(removeEv.Index);
                        break;
                    default:
                        throw new Exception(String.Format("Unexpected event: {0}", ev.EventName));
                }
            }

            Test.AssertEqualJSON(resyncCollection, clone);
        }

        [Fact]
        public async Task Synchronize_WithPrimitiveCollectionlUnchanged_EmitsNoEvents()
        {
            await ConnectAndHandshake();

            var creqTask = Client.SubscribeAsync("test.collection");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.collection");
            req.SendResult(new JObject
            {
                { "collections", new JObject
                    {
                        { "test.collection", Test.Collection }
                    }
                }
            });
            var collection = await creqTask as ResCollection;

            // Disconnect and reconnect
            await WebSocket.DisconnectAsync();
            await ConnectAndHandshake();

            // Expect resynchronization get request
            var completionSource = new TaskCompletionSource<ResourceEventArgs>();
            EventHandler<ResourceEventArgs> h = (object sender, ResourceEventArgs e) => completionSource.SetResult(e);
            collection.ResourceEvent += h;
            var req2 = await WebSocket.GetRequestAsync();
            req2.AssertMethod("subscribe.test.collection");
            // Send identical collection
            req2.SendResult(new JObject
            {
                { "collections", new JObject
                    {
                        { "test.collection", Test.Collection }
                    }
                }
            });

            // Send custom event
            byte[] eventMsg = System.Text.Encoding.UTF8.GetBytes("{\"event\":\"test.collection.custom\",\"data\":null}");
            WebSocket.SendMessage(eventMsg);
            var ev = await completionSource.Task;

            collection.ResourceEvent -= h;

            // Verify we got the custom event and not a change event.
            Assert.Equal("custom", ev.EventName);
        }
    }
}
