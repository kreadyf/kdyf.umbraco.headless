using kdyf.umbraco9.headless.Extensions;
using kdyf.umbraco9.headless.Interfaces;
using kdyf.umbraco9.headless.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace kdyf.umbraco9.headless.Services
{
    public class UmbracoNavigationTreeResolverService : INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings>
    {
        private readonly IMetaPropertyResolverService<IPublishedContent> _metaPropertyResolverService;
        private readonly IContentResolverService<IPublishedContent> _contentResolverService;

        public UmbracoNavigationTreeResolverService(
            IMetaPropertyResolverService<IPublishedContent> metaPropertyResolverService, IContentResolverService<IPublishedContent> contentResolverService)
        {
            _metaPropertyResolverService = metaPropertyResolverService;
            _contentResolverService = contentResolverService;
        }

        public IEnumerable<dynamic> Resolve(IPublishedContent content, NavigationTreeResolverSettings options, SecurityValidationSettings securityoptions = null)
        {
            if (
                options.Depth != 0 && options.ContentDepth > options.Depth || options.ContentDepth == 0 && options.Depth != 0
                )
                throw new ApplicationException($"Invalid parameter: content depth (${options.ContentDepth}) can not be larger than depth (${options.Depth}).");

            return Resolve(content, options.Depth, options.ContentDepth, 1, options.ContentToIncludeInMetaProperties, securityoptions);
        }

        private IEnumerable<dynamic> Resolve(IPublishedContent content, int depth, int contentDepth, int currentDepth, string[] includeInMetaParam, SecurityValidationSettings securityoptions = null)
        {
            if (depth > 0 && depth <= currentDepth)
                return new object[] { };

            return content.Children.Where(n => ValidateAuth(n, securityoptions)).Select(s =>
                DynamicObject.Merge(_metaPropertyResolverService.Resolve(s),
                    _contentResolverService.Resolve(s, includeInMetaParam),
                    contentDepth == 0 || contentDepth > currentDepth ? (object)_contentResolverService.Resolve(s, null) : new { },
                    depth == 0 || depth > currentDepth ? (object)new { Navigation = Resolve(s, depth, contentDepth, currentDepth + 1, includeInMetaParam) } : new { })
            );
        }
        bool ValidateAuth(IPublishedContent n, SecurityValidationSettings securityoptions)
        {
            if (securityoptions == null) return true;
            var result = n.ValidateMemberGroups(securityoptions);
            return result == null ? false : true;
        }

    }
}
