using kdyf.umbraco13.headless.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace kdyf.umbraco9.headless.Extensions
{
    public static class IServiceProviderExtensions
    {
        public static IPublishedContent GetPublishedContentByRoute(this IServiceProvider @this, string url, IEnumerable<IPublishedContent> nodes = null)
        {
            IUmbracoContext umbracoContext = null;
            if (!@this.GetService<IUmbracoContextAccessor>()?.TryGetUmbracoContext(out umbracoContext) ?? false)
                return null;

            IVariationContextAccessor variationContext = @this.GetService<IVariationContextAccessor>();
            if (variationContext == null)
                return null;

            // Original Umbraco behavior - try first
            if (url == null)
                url = "/";

            if (!url.EndsWith('/'))
                url = $"{url}/";

            if (!url.StartsWith("/"))
                url = $"/{url}";

            if (nodes == null)
                nodes = umbracoContext.Content.GetAtRoot();

            var distinctNodes = nodes.GroupBy(n => n.Id).Select(g => g.First()).ToList();

            var content = FindNodeByUrl(distinctNodes, url, variationContext);
            
            // If original routing found content, return it
            if (content != null)
                return content;

            // Original routing returned null/404 - try streamlined culture routing as fallback
            var configuration = @this.GetService<IConfiguration>();
            var streamlineCultureRouting = configuration?.GetValue<bool>("Headless:StreamlineCultureRouting") ?? false;

            if (streamlineCultureRouting)
            {
                var siteCultureResolver = @this.GetService<ISiteCultureResolver>();
                if (siteCultureResolver != null)
                {
                    return siteCultureResolver.ResolveContent(url, variationContext);
                }
            }

            return null;
        }

        private static IPublishedContent FindNodeByUrl(IEnumerable<IPublishedContent> nodes, string url, IVariationContextAccessor variationContext)
        {
            if (nodes == null)
                return null;

            var nodesList = nodes.ToList();

            for (int i = 0; i < nodesList.Count; i++)
            {
                var node = nodesList[i];
                foreach (var item in node.Cultures.Keys)
                {
                    try
                    {
                        var ci = string.IsNullOrEmpty(item) ? null : new CultureInfo(item).ToString();
                        var tmpUrl = string.IsNullOrEmpty(ci) ? node.Url() : node.Url(ci);
                        if (CompareUrl(tmpUrl, url))
                        {
                            if (!string.IsNullOrEmpty(ci))
                                variationContext.VariationContext = new VariationContext(ci);
                            return node;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }

                try
                {
                    if (CompareUrl(node.Url(), url))
                        return node;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                var children = node.Children?.ToList();
                if (children != null && children.Count > 0)
                {
                    var rec = FindNodeByUrl(children, url, variationContext);
                    if (rec != null)
                        return rec;
                }
            }

            return null;
        }

        private static bool CompareUrl(string nodeUrl, string url)
        {
            return nodeUrl.Replace("#", string.Empty).Equals(url, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
