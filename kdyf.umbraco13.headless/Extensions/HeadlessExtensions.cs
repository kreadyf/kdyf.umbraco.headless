using kdyf.umbraco9.headless.Interceptors;
using kdyf.umbraco9.headless.Interfaces;
using kdyf.umbraco9.headless.Models;
using kdyf.umbraco9.headless.Routing;
using kdyf.umbraco9.headless.Services;
using kdyf.umbraco13.headless.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace kdyf.umbraco9.headless.Extensions
{
    public static partial class HeadlessExtensions
    {
        /// <summary>
        /// Adds headless services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Optional configuration to read streamlineCultureRouting setting from.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddHeadless(this IServiceCollection services, IConfiguration configuration = null)
        {
            services.AddScoped<IMetaPropertyResolverService<IPublishedContent>, UmbracoMetaPropertyResolverService>();
            services.AddScoped<IContentResolverService<IPublishedContent>, UmbracoContentResolverService>();
            services.AddScoped<INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings>, UmbracoNavigationTreeResolverService>();

            // Register SiteCultureResolver for streamlined culture routing
            services.AddScoped<ISiteCultureResolver, SiteCultureResolver>();

            var headlessOptions = new UmbracoHeadlessMiddlewareOptions();
            
            // Read streamlineCultureRouting from configuration if provided
            // Configuration path: "Headless:StreamlineCultureRouting" (JSON: "Headless": { "StreamlineCultureRouting": true })
            if (configuration != null)
            {
                var streamlineCultureRouting = configuration.GetValue<bool>("Headless:StreamlineCultureRouting", false);
                headlessOptions.StreamlineCultureRouting = streamlineCultureRouting;
            }
            
            headlessOptions.Interceptors.Add("defaultFolder", new DefaultInterceptor());
            
            services.AddSingleton<IUmbracoHeadlessInterceptorFactory>(_ =>
                new UmbracoHeadlessInterceptorFactory(headlessOptions.Interceptors));

            services.AddRouting(options =>
                options.ConstraintMap.Add("headless", value: typeof(HeadlessRouteConstraint))
            );

            return services;
        }
    }
}
