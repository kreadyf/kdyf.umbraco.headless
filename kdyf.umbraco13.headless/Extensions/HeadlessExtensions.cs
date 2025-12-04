using kdyf.umbraco9.headless.Interceptors;
using kdyf.umbraco9.headless.Interfaces;
using kdyf.umbraco9.headless.Models;
using kdyf.umbraco9.headless.Routing;
using kdyf.umbraco9.headless.Services;
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
        public static IServiceCollection AddHeadless(this IServiceCollection services)
        {
            services.AddScoped<IMetaPropertyResolverService<IPublishedContent>, UmbracoMetaPropertyResolverService>();
            services.AddScoped<IContentResolverService<IPublishedContent>, UmbracoContentResolverService>();
            services.AddScoped<INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings>, UmbracoNavigationTreeResolverService>();

            var headlessOptions = new UmbracoHeadlessMiddlewareOptions();
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
