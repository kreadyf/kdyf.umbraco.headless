using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using Umbraco.Core.Models.PublishedContent;

namespace kdyf.umbraco8.headless.Interfaces
{
    public interface IUmbracoHeadlessInterceptor
    {
        Task<IHttpActionResult> Intercept(IPublishedContent content);
    }
}
