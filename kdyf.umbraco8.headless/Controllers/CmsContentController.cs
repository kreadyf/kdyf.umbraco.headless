using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Mvc;
using HtmlAgilityPack;
using kdyf.umbraco8.headless.Extensions;
using kdyf.umbraco8.headless.Interfaces;
using kdyf.umbraco8.headless.Models;
using kdyf.umbraco8.headless.Services;
using Umbraco.Core;
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
        private readonly IVariationContextAccessor _variationContextAccessor;


        public CmsContentController(
            IContentResolverService<IPublishedContent> contentResolverService,
            IMetaPropertyResolverService<IPublishedContent> metaPropertyResolverService,
            INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings> navigationTreeResolverService,
            UmbracoContext context,
            IVariationContextAccessor variationContextAccessor
            )
        {
            _metaPropertyResolverService = metaPropertyResolverService;
            _contentResolverService = contentResolverService;
            _navigationTreeResolverService = navigationTreeResolverService;
            _context = context;
            _variationContextAccessor = variationContextAccessor;
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
            var content = GetByRouteAndCulture($"/{url}");

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

        // Todo - move logic?
        // Todo - check if caching required?
        // Todo - check if stack better than recursive?
        private IPublishedContent GetByRouteAndCulture(string url, IEnumerable<IPublishedContent> nodes = null)
        {
            if (nodes == null)
            {
                /*var defaultNode = _context.Content.GetByRoute(url);
                if (defaultNode != null)
                    return defaultNode;
                 // will not switch culture if default-route does not use the same culture as default-culture   
                 */

                nodes = _context.Content.GetAtRoot();
            }

            foreach (var node in nodes)
            {
                var variantCulture = node.Cultures.Keys.FirstOrDefault(c => CompareUrl(node.Url(c), url));

                if (variantCulture != null)
                {
                    _variationContextAccessor.VariationContext = new VariationContext(variantCulture);
                    return node;
                }

                if (CompareUrl(node.Url, url))
                    return node;

                var rec = GetByRouteAndCulture(url, node.Children);

                if (rec != null)
                    return rec;
            }

            return null;
        }

        private bool CompareUrl(string nodeUrl, string url)
        {
            return nodeUrl.Equals(url, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
