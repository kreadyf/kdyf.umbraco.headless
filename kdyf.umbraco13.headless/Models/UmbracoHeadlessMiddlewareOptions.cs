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

        /// <summary>
        /// When enabled, routing and content resolution will use the root-node's default culture
        /// instead of the global default culture. URLs like /root-node/xyz will always use the
        /// root-node's default culture, even if it differs from the global default.
        /// </summary>
        public bool StreamlineCultureRouting { get; set; } = false;
    }
}
