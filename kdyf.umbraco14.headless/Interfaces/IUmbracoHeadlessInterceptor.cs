using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace kdyf.umbraco9.headless.Interfaces
{
    public interface IUmbracoHeadlessInterceptor
    {
        Task<IActionResult> Intercept(IPublishedContent content);
    }
}
