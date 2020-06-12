using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Slugify;
using kdyf.umbraco8.headless.Interfaces;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace kdyf.umbraco8.headless.Services
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

            string urlName = slug.GenerateSlug(content.Name);

            string url = string.IsNullOrWhiteSpace(content.Url) || content.Url == "#"
                ? $"#{content.Parent?.Url}{urlName}"
                : content.Url;

            return new {Url = url, UrlName = urlName, content.Name, ContentType = content.ContentType.Alias, content.CreateDate, IsVisible = content.IsVisible()};
        }

    }

}
