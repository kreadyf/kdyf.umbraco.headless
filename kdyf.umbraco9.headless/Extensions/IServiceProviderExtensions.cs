using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace kdyf.umbraco9.headless.Extensions
{
    public static class IServiceProviderExtensions
    {
        public static IPublishedContent GetPublishedContentByRoute(this IServiceProvider @this, string? url, IEnumerable<IPublishedContent> nodes = null)
        {
            IUmbracoContext umbracoContext = null;
            if (!@this.GetService<IUmbracoContextAccessor>()?.TryGetUmbracoContext(out umbracoContext) ?? false)
                return null;

            IVariationContextAccessor variationContext = @this.GetService<IVariationContextAccessor>();
            if (variationContext == null)
                return null;

            if (url == null)
                url = "/";

            if (!url.EndsWith('/'))
                url = $"{url}/";

            if (!url.StartsWith('/'))
                url = $"/{url}";

            if (nodes == null)
                nodes = umbracoContext.Content.GetAtRoot();

            foreach (var node in nodes)
            {
                foreach (var item in node.Cultures.Keys)
                {
                    try
                    {
                        var ci = new CultureInfo(item).ToString();
                        var tmpUrl = node.Url(ci);
                        if (CompareUrl(tmpUrl, url))
                        {
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


                var rec = @this.GetPublishedContentByRoute(url, node.Children);

                if (rec != null)
                    return rec;
            }

            return null;
        }

        private static bool CompareUrl(string nodeUrl, string url)
        {
            return nodeUrl.Replace("#", string.Empty).Equals(url, StringComparison.InvariantCultureIgnoreCase);
        }

    }
}
