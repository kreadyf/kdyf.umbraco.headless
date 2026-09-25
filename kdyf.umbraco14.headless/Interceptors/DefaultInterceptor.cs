using kdyf.umbraco9.headless.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace kdyf.umbraco9.headless.Interceptors
{
    public class DefaultInterceptor : IUmbracoHeadlessInterceptor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<IActionResult> Intercept(IPublishedContent content)
        {
            return new JsonResult(await ResolveContent(content), new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task<object> ResolveContent(IPublishedContent content)
        {
            var res = await Task.Run(() => new { message = "Hello World!" } );

            return res;
        }
    }
}
