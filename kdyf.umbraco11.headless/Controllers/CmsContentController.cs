﻿using kdyf.umbraco9.headless.Attributes;
using kdyf.umbraco9.headless.Extensions;
using kdyf.umbraco9.headless.Helper;
using kdyf.umbraco9.headless.Interfaces;
using kdyf.umbraco9.headless.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.UmbracoContext;
using Umbraco.Extensions;
using kdyf.umbraco9.headless.Constants;

namespace kdyf.umbraco9.headless.Controllers
{

    [Route("")]
    public class CmsContentController : UmbracoApiController
    {
        private readonly IContentResolverService<IPublishedContent> _contentResolverService;
        private readonly IMetaPropertyResolverService<IPublishedContent> _metaPropertyResolverService;
        private readonly INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings> _navigationTreeResolverService;

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        private readonly IUmbracoHeadlessInterceptorFactory _interceptorFactory;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberGroupService _memberGroupService;
        private readonly IMemoryCache _memoryCache;
        readonly IServiceProvider _serviceProvider;

        public CmsContentController(IUmbracoContextAccessor umbracoContextAccessor,
            IMetaPropertyResolverService<IPublishedContent> metaPropertyResolverService,
            IContentResolverService<IPublishedContent> contentResolverService,
            INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings> navigationTreeResolverService,
            IUmbracoHeadlessInterceptorFactory interceptorFactory,
            IHttpContextAccessor httpContextAccessor,
            IMemberGroupService memberGroupService,
            IMemoryCache memoryCache,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _umbracoContextAccessor = umbracoContextAccessor;

            _metaPropertyResolverService = metaPropertyResolverService;
            _contentResolverService = contentResolverService;
            _navigationTreeResolverService = navigationTreeResolverService;

            _interceptorFactory = interceptorFactory;

            _httpContextAccessor = httpContextAccessor;
            _memberGroupService = memberGroupService;
            _memoryCache = memoryCache;
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
            var claimsIdentity = _httpContextAccessor.HttpContext.User.Identities.FirstOrDefault();
            var permissionGroupsInClaim = claimsIdentity.Claims
                .Where(n => n.Type.Equals(PropertyConstants.PermissionGroup, StringComparison.InvariantCultureIgnoreCase))
                .Select(n => n.Value.ToUpper())
                .ToHashSet();

            var isAuthenticated = claimsIdentity.Claims.Any();
            var permissionGroups = _memoryCache.GetOrCreate("PermissionGroups", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var memberGroups = _memberGroupService.GetAll().ToDictionary(k => k.Id, v => v.Key);

                return memberGroups; 
            });

            var authValidation = new SecurityValidationSettings()
            {
                PermissionGroups = permissionGroups,
                IsAuthenticated = isAuthenticated,
                PermissionInClaim = permissionGroupsInClaim,
                ContentResolver = _contentResolverService
            };

            var content = _serviceProvider.GetPublishedContentByRoute(url).ValidateMemberGroups(authValidation);

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
                    new NavigationTreeResolverSettings() { Depth = depth, ContentDepth = contentDepth, ContentToIncludeInMetaProperties = includeInMetaParam },
                    authValidation)
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
    }
}
