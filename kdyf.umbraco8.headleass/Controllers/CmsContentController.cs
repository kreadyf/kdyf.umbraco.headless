using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Mvc;
using kdyf.umbraco8.headless.Extensions;
using kdyf.umbraco8.headless.Interfaces;
using kdyf.umbraco8.headless.Models;
using kdyf.umbraco8.headless.Services;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.WebApi;
using File = System.IO.File;
using Task = System.Threading.Tasks.Task;
using umbracoWeb = Umbraco.Web;

    

namespace kdyf.umbraco8.headless.Controllers
{
    [System.Web.Http.RoutePrefix("")]
    public class CmsContentController : UmbracoApiController
    {
        private readonly IContentResolverService<IPublishedContent> _contentResolverService;
        private readonly IMetaPropertyResolverService<IPublishedContent> _metaPropertyResolverService;
        private readonly INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings> _navigationTreeResolverService;

        private readonly UmbracoContext _context;


        public CmsContentController(
            IContentResolverService<IPublishedContent> contentResolverService,
            IMetaPropertyResolverService<IPublishedContent> metaPropertyResolverService,
            INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings> navigationTreeResolverService,
            UmbracoContext context
            )
        {
            _metaPropertyResolverService = metaPropertyResolverService;
            _contentResolverService = contentResolverService;
            _navigationTreeResolverService = navigationTreeResolverService;
            _context = context;
        }


        /// <summary>Get a content node</summary>
        /// <remarks>Content node contains all information which has entered into the CMS.</remarks>
        /// <returns>Content node: Meta properties, dynamic content and all descendant child nodes including their meta properties.</returns>
        /// <param name="url">The url of the content (if empty or "/" it returns per default not the root node, but the first one)</param>
        /// <param name="depth">Depth of child with meta properties (0 = default: all descendant child nodes)</param>
        /// <param name="contentDepth">Depth of child with complete content (0 = default: all descendant child nodes))</param>
        /// <param name="includeInMeta">Comma separated list of content aliases which should be included in the meta properties</param>
        [System.Web.Http.Route("{*url}")]
        [System.Web.Http.HttpGet]
        public Task<IHttpActionResult> Get(string url = "", int depth = 0, int contentDepth = 1, string includeInMeta = null)
        {


            var content = _context.Content.GetByRoute($"/{url}");

            if (content == null)
                return Task.FromResult(NotFound() as IHttpActionResult);

            string[] includeInMetaParam = string.IsNullOrWhiteSpace(includeInMeta) ? new string[] { } : includeInMeta.Split(',');

            return Task.FromResult(Ok(DynamicObject.Merge(
                _metaPropertyResolverService.Resolve(content),
                _contentResolverService.Resolve(content, null),
                new
                {
                    Navigation = _navigationTreeResolverService.Resolve(content,
                    new NavigationTreeResolverSettings() { Depth = depth, ContentDepth = contentDepth, ContentToIncludeInMetaProperties = includeInMetaParam })
                })) as IHttpActionResult);

        }
    }
}
