using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using uhttpsharp.Listeners;

namespace uhttpsharp.RequestProviders
{
    public class HttpRequestProviderMethodOverrideDecorator : IHttpRequestProvider
    {
        private readonly IHttpRequestProvider _child;

        public HttpRequestProviderMethodOverrideDecorator(IHttpRequestProvider child)
        {
            _child = child;
        }

        public async Task<IHttpRequest> Provide(StreamReader streamReader)
        {
            var childValue = await _child.Provide(streamReader).ConfigureAwait(false);

            if (childValue == null)
            {
                return null;
            }

            string methodName;
            if (!childValue.Headers.TryGetByName("X-HTTP-Method-Override", out methodName))
            {
                return childValue;
            }

            return new HttpRequestMethodDecorator(childValue, HttpMethodProvider.Default.Provide(methodName));
        }

        private readonly IList<IHttpRequestHandler> _handlers = new List<IHttpRequestHandler>();
        private readonly IList<IHttpListener> _listeners = new List<IHttpListener>();

        public void Use(IHttpListener listener)
        {
            _listeners.Add(listener);
        }

        public IList<IHttpListener> getListeners()
        {
            return _listeners;
        }

        public void Use(IHttpRequestHandler handler)
        {
            _handlers.Add(handler);
        }

        public IList<IHttpRequestHandler> getRequestHandlers()
        {
            return _handlers;
        }
    }
}