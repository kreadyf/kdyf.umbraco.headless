using kdyf.umbraco13.headless.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace kdyf.umbraco13.headless.Services
{
    /// <summary>
    /// Resolves content by route using root-node default culture.
    /// </summary>
    public interface ISiteCultureResolver
    {
        /// <summary>
        /// Resolves content by URL, setting the appropriate culture context.
        /// </summary>
        /// <param name="url">The URL path from the request.</param>
        /// <param name="variationContext">The variation context accessor to set the culture.</param>
        /// <returns>The resolved content, or null if resolution fails.</returns>
        IPublishedContent ResolveContent(string url, IVariationContextAccessor variationContext);
    }

    /// <summary>
    /// Implementation of site culture resolver that resolves content using root-node default culture.
    /// </summary>
    public class SiteCultureResolver : ISiteCultureResolver
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IDomainService _domainService;

        public SiteCultureResolver(
            IUmbracoContextAccessor umbracoContextAccessor,
            IDomainService domainService)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _domainService = domainService;
        }

        public IPublishedContent ResolveContent(string url, IVariationContextAccessor variationContext)
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
                return null;

            var urlPath = NormalizeUrl(url).TrimStart('/').TrimEnd('/').ToLowerInvariant();
            var rootNodes = GetAllRootNodes(umbracoContext);
            if (rootNodes.Count == 0)
                return null;

            // Find root node by domain or name
            var (rootNode, relativePath) = FindRootNode(rootNodes, urlPath);
            if (rootNode == null)
                return null;

            // Get and set culture
            var culture = GetRootNodeDefaultCulture(rootNode) ?? rootNode.Cultures?.Keys?.FirstOrDefault();
            if (!string.IsNullOrEmpty(culture))
            {
                try { variationContext.VariationContext = new VariationContext(culture); } catch { }
            }

            // Return root or find child content
            return string.IsNullOrEmpty(relativePath) 
                ? rootNode 
                : FindContentByRelativePath(rootNode, relativePath, culture);
        }

        /// <summary>
        /// Gets all root nodes regardless of culture (includes nodes without current culture).
        /// </summary>
        private List<IPublishedContent> GetAllRootNodes(IUmbracoContext umbracoContext)
        {
            var allRootNodes = new HashSet<IPublishedContent>(new PublishedContentIdEqualityComparer());
            var defaultRootNodes = umbracoContext.Content.GetAtRoot().ToList();
            
            foreach (var node in defaultRootNodes)
                allRootNodes.Add(node);

            // Get cultures from nodes and domains, then fetch root nodes for each
            var allCultures = defaultRootNodes
                .SelectMany(n => n.Cultures?.Keys ?? Enumerable.Empty<string>())
                .Concat(_domainService.GetAll(false)
                    .Where(d => !string.IsNullOrEmpty(d.LanguageIsoCode))
                    .Select(d => d.LanguageIsoCode))
                .Distinct()
                .ToList();

            var currentCulture = umbracoContext.PublishedRequest?.Culture;
            if (!string.IsNullOrEmpty(currentCulture) && !allCultures.Contains(currentCulture))
                allCultures.Add(currentCulture);

            foreach (var cultureCode in allCultures)
            {
                try
                {
                    var cultureRootNodes = umbracoContext.Content.GetAtRoot(cultureCode);
                    if (cultureRootNodes != null)
                    {
                        foreach (var node in cultureRootNodes)
                            allRootNodes.Add(node);
                    }
                }
                catch { }
            }

            return allRootNodes.ToList();
        }

        /// <summary>
        /// Finds root node and extracts relative path from URL.
        /// </summary>
        private (IPublishedContent rootNode, string relativePath) FindRootNode(List<IPublishedContent> rootNodes, string urlPath)
        {
            IPublishedContent rootDomainNode = null;

            // First pass: Try specific domain matching (excluding root "/" domain)
            // This ensures paths like "/spain/news" match "/spain/" domain, not "/" domain
            foreach (var node in rootNodes)
            {
                foreach (var domain in _domainService.GetAssignedDomains(node.Id, false))
                {
                    if (string.IsNullOrEmpty(domain.DomainName))
                        continue;

                    // Skip root domain "/" in first pass - check it later as fallback
                    if (domain.DomainName == "/")
                    {
                        // Store first root domain node found (typically only one exists)
                        if (rootDomainNode == null)
                            rootDomainNode = node;
                        continue;
                    }

                    var domainPath = ExtractDomainPath(domain.DomainName);
                    if (string.IsNullOrEmpty(domainPath))
                        continue;

                    // Exact match: URL path equals domain path
                    if (urlPath == domainPath)
                        return (node, string.Empty);
                    
                    // Prefix match: URL path starts with domain path + "/"
                    if (!string.IsNullOrEmpty(urlPath) && urlPath.StartsWith(domainPath + "/", StringComparison.OrdinalIgnoreCase))
                        return (node, urlPath.Substring(domainPath.Length + 1));
                }
            }

            // Second pass: Check root domain "/" only if no specific domain matched
            // Root domain is the fallback for paths that don't match any specific domain
            if (rootDomainNode != null)
            {
                // Empty path: return root node
                if (string.IsNullOrEmpty(urlPath))
                    return (rootDomainNode, string.Empty);
                // Non-empty path: treat entire path as relative path under root domain
                return (rootDomainNode, urlPath);
            }

            // Fallback: match by node name
            var segments = urlPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                var rootNode = rootNodes.FirstOrDefault(n => n.Name.Equals(segments[0], StringComparison.InvariantCultureIgnoreCase));
                if (rootNode != null)
                    return (rootNode, string.Join("/", segments.Skip(1)));
            }

            // Final fallback: if root URL "/" and no domain matched, return first root node
            if (string.IsNullOrEmpty(urlPath) && rootNodes.Count > 0)
                return (rootNodes.First(), string.Empty);

            return (null, null);
        }

        /// <summary>
        /// Normalizes a URL by ensuring it starts and ends with "/".
        /// </summary>
        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "/";

            if (!url.StartsWith("/"))
                url = "/" + url;

            if (!url.EndsWith("/"))
                url = url + "/";

            return url;
        }

        /// <summary>
        /// Finds content by relative path starting from a root node.
        /// </summary>
        private static IPublishedContent FindContentByRelativePath(IPublishedContent rootNode, string relativePath, string culture)
        {
            if (rootNode == null || string.IsNullOrEmpty(relativePath))
                return rootNode;

            var segments = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return rootNode;

            IPublishedContent current = rootNode;
            foreach (var segment in segments)
            {
                var children = current?.Children?.ToList();
                if (children == null || children.Count == 0)
                    return null;

                IPublishedContent found = null;
                foreach (var child in children)
                {
                    // Try to get URL with culture, fallback to default
                    string childUrl = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(culture) && child.Cultures.ContainsKey(culture))
                        {
                            childUrl = child.Url(culture);
                        }
                        else
                        {
                            childUrl = child.Url();
                        }
                    }
                    catch
                    {
                        try { childUrl = child.Url(); } catch { }
                    }

                    if (string.IsNullOrEmpty(childUrl))
                        continue;

                    // Extract last segment from URL
                    var urlSegments = childUrl.TrimStart('/').TrimEnd('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var lastSegment = urlSegments.Length > 0 ? urlSegments[urlSegments.Length - 1] : null;

                    // Match by URL segment or name
                    if (lastSegment != null && 
                        (lastSegment.Equals(segment, StringComparison.InvariantCultureIgnoreCase) ||
                         child.Name.Equals(segment, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        found = child;
                        break;
                    }
                }

                if (found == null)
                    return null;

                current = found;
            }

            return current;
        }

        /// <summary>
        /// Extracts and normalizes the domain path from a domain name.
        /// Handles formats like "/spain/", "spain", "example.com/spain", etc.
        /// </summary>
        private string ExtractDomainPath(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
                return null;

            // Remove leading/trailing slashes and convert to lowercase
            domainName = domainName.Trim().TrimStart('/').TrimEnd('/');

            // If domain contains "/", it might be a full domain with path (e.g., "example.com/spain")
            // Extract just the last segment (the path part)
            if (domainName.Contains("/"))
            {
                var parts = domainName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    // Return the last part (the path segment)
                    return parts[parts.Length - 1].ToLowerInvariant();
                }
            }

            // Simple path name (e.g., "spain")
            return domainName.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the default culture for a root node from its domain configuration.
        /// </summary>
        private string GetRootNodeDefaultCulture(IPublishedContent rootNode)
        {
            if (rootNode == null)
            {
                return null;
            }

            // Get all domains assigned to this root node
            var domains = _domainService.GetAssignedDomains(rootNode.Id, false).ToList();

            if (domains == null || domains.Count == 0)
            {
                return null;
            }

            // Find the default domain (usually the first one, or one marked as default)
            // In Umbraco, domains are typically ordered with the default first
            var defaultDomain = domains.FirstOrDefault();

            if (defaultDomain != null)
            {
                // In Umbraco 13, domains use LanguageIsoCode property
                var cultureCode = defaultDomain.LanguageIsoCode;

                if (!string.IsNullOrEmpty(cultureCode))
                {
                    try
                    {
                        // Normalize culture code
                        var cultureInfo = new CultureInfo(cultureCode);
                        return cultureInfo.Name;
                    }
                    catch
                    {
                        // If culture parsing fails, return as-is
                        return cultureCode;
                    }
                }
            }

            return null;
        }
    }
}

