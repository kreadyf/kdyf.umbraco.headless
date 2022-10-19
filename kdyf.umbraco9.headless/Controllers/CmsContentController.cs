using kdyf.umbraco9.headless.Attributes;
using kdyf.umbraco9.headless.Extensions;
using kdyf.umbraco9.headless.Helper;
using kdyf.umbraco9.headless.Interfaces;
using kdyf.umbraco9.headless.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.UmbracoContext;
using Umbraco.Extensions;

namespace kdyf.umbraco9.headless.Controllers
{
    
    [Route("")]
    public class CmsContentController : UmbracoApiController
    {
        private readonly IContentResolverService<IPublishedContent> _contentResolverService;
        private readonly IMetaPropertyResolverService<IPublishedContent> _metaPropertyResolverService;
        private readonly INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings> _navigationTreeResolverService;

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IVariationContextAccessor _variationContextAccessor;

        private readonly IUmbracoHeadlessInterceptorFactory _interceptorFactory;

        public CmsContentController(IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            IMetaPropertyResolverService<IPublishedContent> metaPropertyResolverService,
            IContentResolverService<IPublishedContent> contentResolverService,
            INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings> navigationTreeResolverService,
            IUmbracoHeadlessInterceptorFactory interceptorFactory
            )
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _variationContextAccessor = variationContextAccessor;

            _metaPropertyResolverService = metaPropertyResolverService;
            _contentResolverService = contentResolverService;
            _navigationTreeResolverService = navigationTreeResolverService;

            _interceptorFactory = interceptorFactory;
        }

        /// <summary>Get a content node</summary>
        /// <remarks>Content node contains all information which has entered into the CMS.</remarks>
        /// <returns>Content node: Meta properties, dynamic content and all descendant child nodes including their meta properties.</returns>
        /// <param name="url">The url of the content (if empty or "/" it returns per default not the root node, but the first one)</param>
        /// <param name="depth">Depth of child with meta properties (0 = default: all descendant child nodes)</param>
        /// <param name="contentDepth">Depth of child with complete content (0 = default: all descendant child nodes))</param>
        /// <param name="includeInMeta">Comma separated list of content aliases which should be included in the meta properties</param>
        [Route("{*url:headless}")]
        [HttpGet]
        public async Task<IActionResult> Get(string url = "", int depth = 0, int contentDepth = 1, string includeInMeta = null)
        {
            
            var urlFixed = fixUrl(url);
            var content = GetByRouteAndCulture(urlFixed);

            if (content == null)
                return NotFound();

            var interceptor = _interceptorFactory.GetInterceptorByDocumentTypeAlias(content.ContentType.Alias);

            if (interceptor != null)
                return await interceptor.Intercept(content);

            string[] includeInMetaParam = string.IsNullOrWhiteSpace(includeInMeta) ? new string[] { } : includeInMeta.Split(',');

            var properties = _metaPropertyResolverService.Resolve(content);
            var contentResolv = _contentResolverService.Resolve(content, null);
            var navigation = new
            {
                Navigation = _navigationTreeResolverService.Resolve(content,
                    new NavigationTreeResolverSettings() { Depth = depth, ContentDepth = contentDepth, ContentToIncludeInMetaProperties = includeInMetaParam })
            };

            return Ok(DynamicObject.Merge(
                properties,
                contentResolv,
                navigation)) as IActionResult;
        }

        private string fixUrl(string url)
        {
            string result = url ?? "/";

            if (!result.StartsWith("/"))
            {
                result = "/" + result;
            }

            if (!result.EndsWith("/"))
            {
                result = result + "/";
            }

            return result;
        }

        private IPublishedContent GetByRouteAndCulture(string url, IEnumerable<IPublishedContent> nodes = null)
        {
            if (nodes == null)
            {
                /*var defaultNode = _context.Content.GetByRoute(url);
                if (defaultNode != null)
                    return defaultNode;
                 // will not switch culture if default-route does not use the same culture as default-culture   
                 */
                var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();

                nodes = umbracoContext.Content.GetAtRoot();
            }

            foreach (var node in nodes)
            {
                //var variantCulture = node.Cultures.Keys.FirstOrDefault(c => CompareUrl(node.Url(c.FixCulture()), url));

                foreach (var item in node.Cultures.Keys)
                {
                    try
                    {
                        var ci = new CultureInfo(item).ToString();
                        var tmpUrl = node.Url(ci);
                        if (CompareUrl(tmpUrl, url))
                        {
                            _variationContextAccessor.VariationContext = new VariationContext(ci);
                            return node;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }

                //if (variantCulture != null)
                //{
                //    _variationContextAccessor.VariationContext = new VariationContext(variantCulture.FixCulture());
                //    return node;
                //}

                try
                {
                    if (CompareUrl(node.Url(), url))
                        return node;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                

                var rec = GetByRouteAndCulture(url, node.Children);

                if (rec != null)
                    return rec;
            }

            return null;
        }

        private bool CompareUrl(string nodeUrl, string url)
        {
            return nodeUrl.Replace("#", string.Empty).Equals(url, StringComparison.InvariantCultureIgnoreCase);
        }


    }


}
