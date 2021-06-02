using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using uhttpsharp.Listeners;

namespace uhttpsharp.RequestProviders
{
    public interface IHttpRequestProvider
    {
        /// <summary>
        /// Provides an <see cref="IHttpRequest"/> based on the context of the stream,
        /// May return null / throw exceptions on invalid requests.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        Task<IHttpRequest> Provide(StreamReader streamReader);

        void Use(IHttpRequestHandler handler);
        void Use(IHttpListener listener);
        IList<IHttpListener> getListeners();
        IList<IHttpRequestHandler> getRequestHandlers();

    }
}