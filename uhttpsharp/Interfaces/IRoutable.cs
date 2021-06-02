using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace uhttpsharp.Interfaces
{
    public interface IRoutable
    {
        Dictionary<string, Func<IHttpContext, Dictionary<string, string>, Func<Task>, Task>> GetRoutes();
    }
}
