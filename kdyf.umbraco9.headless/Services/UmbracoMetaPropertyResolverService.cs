
using kdyf.umbraco9.headless.Interfaces;
using Slugify;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace kdyf.umbraco9.headless.Services
{
    public class UmbracoMetaPropertyResolverService : IMetaPropertyResolverService<IPublishedContent>
    {
        private static SlugHelper slug = new SlugHelper();


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

            return new { Url = url, UrlName = urlName, content.Name, ContentType = content.ContentType.Alias, content.CreateDate, content.UpdateDate, IsVisible = content.IsVisible() };
        }

    }
}
