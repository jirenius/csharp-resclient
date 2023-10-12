using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResgateIO.Client
{
    public interface IResClient : IDisposable
    {
        event EventHandler<ResourceEventArgs> ResourceEvent;
        event ErrorEventHandler Error;
        string ResgateProtocol { get; }
        bool Connected { get; }
        ResClient SetSerializerSettings(JsonSerializerSettings settings);
        ResClient SetOnConnect(Func<ResClient, Task> callback);
        ResClient SetReconnectDelay(int milliseconds);
        ResClient SetReconnectDelay(TimeSpan duration);
        Task ConnectAsync();
        Task DisconnectAsync();
        Task<object> AuthAsync(string rid, string method);
        Task<object> AuthAsync(string rid, string method, object parameters);
        Task<T> AuthAsync<T>(string rid, string method);
        Task<T> AuthAsync<T>(string rid, string method, object parameters);
        Task<object> CallAsync(string rid, string method);
        Task<object> CallAsync(string rid, string method, object parameters);
        Task<T> CallAsync<T>(string rid, string method);
        Task<T> CallAsync<T>(string rid, string method, object parameters);
        Task<ResResource> SubscribeAsync(string rid);
        Task UnsubscribeAsync(string rid);
        void RegisterModelFactory(string pattern, ModelFactory factory);
        void RegisterCollectionFactory(string pattern, CollectionFactory factory);
    }
}