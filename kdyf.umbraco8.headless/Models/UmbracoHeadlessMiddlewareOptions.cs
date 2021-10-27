using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kdyf.umbraco8.headless.Interfaces;

namespace kdyf.umbraco8.headless.Models
{
    public class UmbracoHeadlessMiddlewareOptions
    {
        public Dictionary<string, IUmbracoHeadlessInterceptor> Interceptors { get; } =
            new Dictionary<string, IUmbracoHeadlessInterceptor>();
    }
}
