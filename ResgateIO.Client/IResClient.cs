using System.Threading.Tasks;

namespace ResgateIO.Client
{
    public interface IResClient
    {
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
    }
}