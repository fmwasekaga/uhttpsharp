using System;
using System.Threading.Tasks;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;

namespace uhttpsharp
{
    public static class HttpServerExtensions
    {

        public static void  Use(this HttpServer server, Func<IHttpContext, Func<Task>, Task> method)
        {
            server.Use(new AnonymousHttpRequestHandler(method));
        }

        public static IHttpRequestProvider Uses(this IHttpRequestProvider provider, IHttpListener listener)
        {
            provider.Use(listener);
            return provider;
        }

        public static IHttpRequestProvider Uses(this IHttpRequestProvider provider, IHttpRequestHandler handler)
        {
            provider.Use(handler);
            return provider;
        }

    }

    public class AnonymousHttpRequestHandler : IHttpRequestHandler
    {
        private readonly Func<IHttpContext, Func<Task>, Task> _method;

        public AnonymousHttpRequestHandler(Func<IHttpContext, Func<Task>, Task> method)
        {
            _method = method;
        }


        public Task Handle(IHttpContext context, Func<Task> next)
        {
            return _method(context, next);
        }
    }
}
