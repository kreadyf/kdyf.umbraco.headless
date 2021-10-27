using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using LightInject;
using LightInject.Mvc;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NPoco.Expressions;
using Owin;
using kdyf.umbraco8.headless.Controllers;
using kdyf.umbraco8.headless.Interfaces;
using kdyf.umbraco8.headless.Models;
using kdyf.umbraco8.headless.Services;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using File = Umbraco.Core.Models.File;

namespace kdyf.umbraco8.headless
{
    public static class HeadlessUmbracoCmsMiddleware
    {
        public static HttpConfiguration UseHeadlessUmbacoCms(this IAppBuilder app, Action<UmbracoHeadlessMiddlewareOptions> options = null)
        {
            UmbracoHeadlessMiddlewareOptions headlessOptions = new UmbracoHeadlessMiddlewareOptions();

            if ( options != null)
                options(headlessOptions);
            
            HttpConfiguration config = new HttpConfiguration();

            var container = new ServiceContainer();

            container.Register<IMetaPropertyResolverService<IPublishedContent>, UmbracoMetaPropertyResolverService>();
            container.Register<IContentResolverService<IPublishedContent>, UmbracoContentResolverService>();
            container.Register<INavigationTreeResolverService<IPublishedContent, NavigationTreeResolverSettings>, UmbracoNavigationTreeResolverService>();

            container.RegisterSingleton<IUmbracoHeadlessInterceptorFactory>(_ =>
                new UmbracoHeadlessInterceptorFactory(headlessOptions.Interceptors));

            container.RegisterScoped<UmbracoContext>(d => Umbraco.Web.Composing.Current.UmbracoContext);
            container.RegisterScoped<IVariationContextAccessor>(d => Umbraco.Web.Composing.Current.VariationContextAccessor);


            container.RegisterApiControllers(typeof(CmsContentController).Assembly);
            container.EnableWebApi(config);

            // JSON Options
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            config.Formatters.JsonFormatter.SerializerSettings.Converters = new List<JsonConverter>
            {
                new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy(true, true, true)},
            };

            // Routes
            config.Routes.IgnoreRoute("Umbraco_Resources", "{resource}.axd/{*pathInfo}");
            config.Routes.IgnoreRoute("Umbraco_Lib", "lib/{*pathInfo}");
            config.Routes.IgnoreRoute("Umbraco_Backend", "umbraco/{*pathInfo}");
            config.Routes.IgnoreRoute("Umbraco_Media", "media/{*pathInfo}");
            config.Routes.IgnoreRoute("Umbraco_Client", "umbraco_client/{*pathInfo}");
            config.Routes.IgnoreRoute("Umbraco_Plugins", "App_Plugins/{*pathInfo}");
            config.Routes.IgnoreRoute("Favicon", "favicon.ico");

            config.MapHttpAttributeRoutes();

            app.UseWebApi(config);

            return config;
        }
    }
}
