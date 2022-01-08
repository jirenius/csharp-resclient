using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ResgateIO.Client.UnitTests
{
    public class MockRequest
    {
        public readonly MockWebSocket WebSocket;

        public readonly int Id;

        public readonly string Method;

        public readonly JToken Params;

        public readonly JToken Token;

        public MockRequest(MockWebSocket webSocket, byte[] data)
        {
            WebSocket = webSocket;
            var msg = Encoding.UTF8.GetString(data);
            var token = JToken.Parse(msg);
            var req = token.ToObject<RequestDto>();
            Id = req.Id;
            Method = req.Method;
            Params = req.Params;
            Token = token;
        }

        public void SendResult(object result)
        {
            var dta = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ResponseDto
            {
                Id = Id,
                Result = result,
            }));

            WebSocket.SendMessage(dta);
        }

        public void SendError(ResError err)
        {
            var dta = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ResponseDto
            {
                Id = Id,
                Error = err,
            }));

            WebSocket.SendMessage(dta);
        }

        public MockRequest AssertMethod(string expectedMethod)
        {
            Assert.Equal(expectedMethod, Method);
            return this;
        }

        public MockRequest AssertParams(object expectedParams)
        {
            JToken expected = JToken.FromObject(expectedParams);
            Assert.True(JToken.DeepEquals(JToken.FromObject(expectedParams), Params), String.Format("Expected JSON: {0}\nActual JSON:   {1}", expected.ToString(), Params.ToString()));
            return this;
        }
    }
}
