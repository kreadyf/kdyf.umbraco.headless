using kdyf.umbraco9.headless.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kdyf.umbraco9.headless.Models
{
    public class UmbracoHeadlessMiddlewareOptions
    {
        public Dictionary<string, IUmbracoHeadlessInterceptor> Interceptors { get; } =
            new Dictionary<string, IUmbracoHeadlessInterceptor>();
    }
}
