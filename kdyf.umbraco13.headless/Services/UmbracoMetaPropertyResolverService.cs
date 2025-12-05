
using kdyf.umbraco9.headless.Interfaces;
using Slugify;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace kdyf.umbraco9.headless.Services
{
    public class UmbracoMetaPropertyResolverService : IMetaPropertyResolverService<IPublishedContent>
    {
        private static SlugHelper slug = new SlugHelper();
        private readonly IServiceProvider _serviceProvider;

        public UmbracoMetaPropertyResolverService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public dynamic Resolve(IPublishedContent content)
        {
            // for nested content items
            /*string urlName = string.IsNullOrWhiteSpace(content.UrlName)
                ? slug.GenerateSlug(content.Name)
                : content.UrlName;*/

            string urlName = slug.GenerateSlug(content.Name ?? string.Empty)
                .Replace(".", string.Empty);

            string url = string.IsNullOrWhiteSpace(content.Url()) || content.Url() == "#"
                ? $"#{content.Parent?.Url()}{urlName}"
                : content.Url();

            // Get culture from variation context (set during routing) or content
            string culture = null;
            
            // Try to get from variation context first (set by streamlined routing)
            try
            {
                var variationContextAccessor = _serviceProvider?.GetService<IVariationContextAccessor>();
                culture = variationContextAccessor?.VariationContext?.Culture;
            }
            catch { }
            
            // Fallback: get culture from content's cultures
            if (string.IsNullOrEmpty(culture) && content.Cultures != null && content.Cultures.Keys.Any())
            {
                // Try to match URL with culture-specific URLs
                foreach (var cultureKey in content.Cultures.Keys)
                {
                    try
                    {
                        var cultureUrl = content.Url(cultureKey);
                        if (cultureUrl == url || (!string.IsNullOrEmpty(cultureUrl) && url.Contains(cultureUrl)))
                        {
                            culture = cultureKey;
                            break;
                        }
                    }
                    catch { }
                }
                
                // Final fallback: use first culture
                if (string.IsNullOrEmpty(culture))
                {
                    culture = content.Cultures.Keys.FirstOrDefault();
                }
            }

            return new { 
                Url = url, 
                UrlName = urlName, 
                content.Name, 
                ContentType = content.ContentType.Alias, 
                content.CreateDate, 
                content.UpdateDate, 
                IsVisible = content.IsVisible(),
                Culture = culture
            };
        }

    }
}
