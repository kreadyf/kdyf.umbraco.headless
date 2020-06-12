using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using kdyf.umbraco8.headless.Extensions;
using kdyf.umbraco8.headless.Interfaces;
using Lucene.Net.Support;
using NPoco;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors.ValueConverters;
using Umbraco.Web;

namespace kdyf.umbraco8.headless.Services
{
    public class UmbracoContentResolverService : IContentResolverService<IPublishedContent>
    {
        private readonly IMetaPropertyResolverService<IPublishedContent> _metaPropertyResolverService;
        private readonly UmbracoContext _umbContext;

        public UmbracoContentResolverService(IMetaPropertyResolverService<IPublishedContent> metaPropertyResolverService, UmbracoContext umbContext)
        {
            _metaPropertyResolverService = metaPropertyResolverService;
            _umbContext = umbContext;
        }

        private class PropertyNameComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return string.IsNullOrWhiteSpace(x) && string.IsNullOrWhiteSpace(y) ||
                       x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return string.IsNullOrWhiteSpace(obj) ? 0 : obj.GetHashCode();
            }
        }

        private static readonly PropertyNameComparer propertyNameComparer = new PropertyNameComparer();

        public dynamic Resolve(IPublishedContent content, string [] aliases)
        {
            var res = content.Properties
                .Where(s => aliases == null || aliases.Contains(s.PropertyType.Alias, propertyNameComparer))
                .ToDictionary(
                    k => k.PropertyType.Alias,
                    v => ResolveProperty(content.Value<dynamic>(v.PropertyType.Alias), v.PropertyType.Alias));

            // @ carlos add all culture urls (test it)
            var allCultures = content.Cultures.Keys;
            if ( allCultures.Count() > 1 && !string.IsNullOrEmpty(allCultures.FirstOrDefault()) )
                res.Add("Languages", allCultures.ToDictionary(k=>k, v=>content.Url(v)));
                
            return res.ToDynamicObject();
        }

        private dynamic Resolve(IPublishedElement content, string[] aliases)
        {
            var res = content.Properties
                .Where(s => aliases == null || aliases.Contains(s.PropertyType.Alias, propertyNameComparer))
                .ToDictionary(
                    k => k.PropertyType.Alias,
                    v => ResolveProperty(content.Value<dynamic>(v.PropertyType.Alias), v.PropertyType.Alias));

            return res.ToDynamicObject();
        }

        private dynamic Resolve(IPublishedContent content)
        {
            if (content == null)
                return null;

            if ( content.ContentType.Alias == "Image")
                return ResolveProperty(content, "Image");

            return DynamicObject.Merge(_metaPropertyResolverService.Resolve(content), Resolve(content, null));
        }

        private dynamic ResolveProperty(dynamic propertyValue, string contentType)
        {
            string strType =  propertyValue?.GetType().ToString(); // could be generated runtime type

            if (string.IsNullOrWhiteSpace(strType))
                return null;

            

            if (contentType == "Image" || strType.EndsWith(".Image"))
            {
                var publishedContent = propertyValue as IPublishedContent;

                var crop = publishedContent.Value<ImageCropperValue>("umbracoFile");

                return new
                {
                    publishedContent.Url,
                    publishedContent.Name,
                    publishedContent.CreateDate,
                    ContentType = "Image",
                    Width = publishedContent.Value<int>("umbracoWidth"),
                    Height = publishedContent.Value<int>("umbracoHeight"),
                    Bytes = publishedContent.Value<int>("umbracoBytes"),
                    Extension = publishedContent.Value<string>("umbracoExtension"),
                    Focal = crop.HasFocalPoint() ? new { crop.FocalPoint.Left, crop.FocalPoint.Top } : new {Left = (decimal)0.5, Top = (decimal)0.5}
                };
            }

            if (strType.EndsWith("HtmlString"))
            {
                return (propertyValue as IHtmlString)?.ToHtmlString();
            }


            if (strType == "Umbraco.Core.Udi[]")
            {
                var udiList = propertyValue as Udi[];

                return udiList.Any()
                    ? udiList // should not happen - handled before as list of IPublishedContent or single image
                    : null; // if data type is a single image - but empty - it would also return an empty array which is not convenient on the frontend
            }


            // @Carlos more generic with nullables, ienumerable<x> etc. and maybe some property converters
            // check if type is primitive or nullable of primitive or ienumerable of primitive
            var type = ((object)propertyValue).GetType();
            if (IsNoneComplexType(type) || IsNoneComplexType(GetEnumerableUnderlyingType(type)))
                return propertyValue;

            if (strType == "Newtonsoft.Json.Linq.JObject")
                return (propertyValue as Newtonsoft.Json.Linq.JObject);

            if ( propertyValue is IPublishedContent )
                return Resolve(propertyValue);

            if (IsOfAnyType(GetEnumerableUnderlyingType(type), typeof(IPublishedContent)))
                return (propertyValue as IEnumerable<IPublishedContent>)?.Select(s => Resolve(s)).Where(s => s != null);

            if (propertyValue is IPublishedElement)
                return Resolve(propertyValue as IPublishedElement, null);

            if (IsOfAnyType(GetEnumerableUnderlyingType(type), typeof(IPublishedElement)))
                return (propertyValue as IEnumerable<IPublishedElement>).Select(s => Resolve(s, null));

            return $"{strType}::{string.Join(",", GetEnumerableUnderlyingType(type)?.GetInterfaces()?.Select(s=>s.Name))}" ;
        }

        private static bool IsNoneComplexType(Type type)
        {
            return (type != null) && 
                (type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(string) || type == typeof(TimeSpan) || Nullable.GetUnderlyingType(type) != null);
        }

        private static bool IsOfAnyType(Type type, Type compareType)
        {
            return (type != null) &&
                    (type == compareType || type.GetInterfaces().Contains(compareType) || type.BaseType == compareType || (type.BaseType?.GetInterfaces()?.Contains(compareType) ?? false));
        }

        private static Type GetEnumerableUnderlyingType(Type type)
        {
            if (type == typeof(string))
                return null;

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? null;
        }
    }

}
