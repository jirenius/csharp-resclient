using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResgateIO.Client.UnitTests
{
    public class ResErrorTests : TestsBase
    {
        public ResErrorTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task SubscribeAsync_WithReferenceToError_GetsModel()
        {
            await ConnectAndHandshake();

            var creqTask = Client.SubscribeAsync("test.model");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.model");
            req.SendResult(new JObject
            {
                { "models", new JObject
                    {
                        { "test.model", new JObject { { "foo", new JObject { { "rid", "test.timeout" } } } } },
                    }
                },
                { "errors", new JObject
                    {
                        { "test.timeout", Test.Resources["test.timeout"] },
                    }
                }
            });
            var result = await creqTask;
            Assert.Equal("test.model", result.ResourceID);
            Assert.IsType<ResModel>(result);

            var model = result as ResModel;
            Assert.IsType<ResResourceError>(model["foo"]);
            Assert.Equal(ResError.CodeTimeout, (model["foo"] as ResResourceError).Error.Code);
        }

        [Fact]
        public async Task SubscribeAsync_WithReferenceToError_GetsCollection()
        {
            await ConnectAndHandshake();

            var creqTask = Client.SubscribeAsync("test.collection");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.collection");
            req.SendResult(new JObject
            {
                { "collections", new JObject
                    {
                        { "test.collection", new JArray { new JObject { { "rid", "test.timeout" } } } },
                    }
                },
                { "errors", new JObject
                    {
                        { "test.timeout", Test.Resources["test.timeout"] },
                    }
                }
            });
            var result = await creqTask;
            Assert.Equal("test.collection", result.ResourceID);
            Assert.IsType<ResCollection>(result);

            var collection = result as ResCollection;
            Assert.Single(collection);
            Assert.IsType<ResResourceError>(collection[0]);
            Assert.Equal(ResError.CodeTimeout, (collection[0] as ResResourceError).Error.Code);
        }


        [Fact]
        public async Task SubscribeAsync_WithReferenceToCustomModelError_ReferencesModelWithError()
        {
            Client.RegisterModelFactory("test.custom.*", (client, rid) => new MockModel(client, rid));
            Client.RegisterCollectionFactory("test.custom", (client, rid) => new ResCollection<MockModel>(client, rid));
            await ConnectAndHandshake();

            var creqTask = Client.SubscribeAsync("test.custom");
            var req = await WebSocket.GetRequestAsync();
            req.AssertMethod("subscribe.test.custom");
            req.SendResult(new JObject
            {
                { "collections", new JObject
                    {
                        { "test.custom", new JArray { new JObject { { "rid", "test.custom.timeout" } } } }
                    }
                },
                { "errors", new JObject
                    {
                        { "test.custom.timeout", Test.Resources["test.timeout"] },
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

            Assert.NotNull(model.Error);
            Assert.Equal(ResError.CodeTimeout, model.Error.Code);
            Assert.Equal("test.custom.timeout", model.ResourceID);
        }
    }
}
