

using kdyf.umbraco9.headless.Extensions;
using kdyf.umbraco9.headless.Helper;
using kdyf.umbraco9.headless.Interfaces;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.UmbracoContext;
using Umbraco.Extensions;

namespace kdyf.umbraco9.headless.Services
{
    public class UmbracoContentResolverService : IContentResolverService<IPublishedContent>
    {
        private readonly IMetaPropertyResolverService<IPublishedContent> _metaPropertyResolverService;


        public UmbracoContentResolverService(IMetaPropertyResolverService<IPublishedContent> metaPropertyResolverService)
        {
            _metaPropertyResolverService = metaPropertyResolverService;
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

        public dynamic Resolve(IPublishedContent content, string[] aliases)
        {
            var res = content.Properties
                .Where(s => aliases == null || aliases.Contains(s.PropertyType.Alias, propertyNameComparer))
                .ToDictionary(
                    k => k.PropertyType.Alias,
                    v => ResolveProperty(content.Value<dynamic>(v.PropertyType.Alias), v.PropertyType.Alias));

            var allCultures = content.Cultures.Keys;
            //if (allCultures.Count() > 1 && !string.IsNullOrEmpty(allCultures.FirstOrDefault()))
            //    res.Add("Languages", allCultures.ToDictionary(k => k, v => content.Url(v.FixCulture())));

            if (allCultures.Count() > 0 && !string.IsNullOrEmpty(allCultures.FirstOrDefault()))
            {
                var langDic = new Dictionary<string, string>();

                foreach (var alias in allCultures)
                {
                    try
                    {
                        var ci = new CultureInfo(alias).ToString();
                        langDic.Add(alias, content.Url(ci));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }

                res.Add("Languages", langDic);
            }

            return res.ToDynamicObject();
        }

        private dynamic ResolveLink(Umbraco.Cms.Core.Models.Link content, string[] aliases)
        {
            return null;
        }
        private dynamic ResolveItemFromList(IPublishedElement content, string[] aliases)
        {
            var res = content.Properties
                .Where(s => aliases == null || aliases.Contains(s.PropertyType.Alias, propertyNameComparer))
                .ToDictionary(
                    k => k.PropertyType.Alias,
                    v => ResolveProperty(content.Value<dynamic>(v.PropertyType.Alias), v.PropertyType.Alias));

            res.Add("ContentType", content.ContentType.Alias);

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

        private static List<dynamic> ResolveLinks(dynamic propertyValue)
        {
            var linkList = new List<dynamic>();
            foreach (var item in (propertyValue as IEnumerable<Umbraco.Cms.Core.Models.Link>))
            {
                linkList.Add(new { item.Name, item.Type, item.Url, item.Target });
            }

            return linkList;
        }

        private dynamic Resolve(IPublishedContent content)
        {
            if (content == null)
                return null;

            if (content.ContentType.Alias == "Image")
                return ResolveProperty(content, "Image");

            return DynamicObject.Merge(_metaPropertyResolverService.Resolve(content), Resolve(content, null));
        }

        private dynamic ResolveProperty(dynamic propertyValue, string contentType)
        {
            string strType = propertyValue?.GetType().ToString(); // could be generated runtime type

            if (string.IsNullOrWhiteSpace(strType))
                return null;



            if (contentType == "Image" || strType.EndsWith(".Image"))
            {
                var publishedContent = propertyValue as IPublishedContent;

                var crop = publishedContent.Value<ImageCropperValue>("umbracoFile");

                return new
                {
                    Url = publishedContent.Url(),
                    publishedContent.Name,
                    publishedContent.CreateDate,
                    ContentType = "Image",
                    Width = publishedContent.Value<int>("umbracoWidth"),
                    Height = publishedContent.Value<int>("umbracoHeight"),
                    Bytes = publishedContent.Value<int>("umbracoBytes"),
                    Extension = publishedContent.Value<string>("umbracoExtension"),
                    Focal = crop.HasFocalPoint() ? new { crop.FocalPoint.Left, crop.FocalPoint.Top } : new { Left = (decimal)0.5, Top = (decimal)0.5 }
                };
            }

            if (strType.EndsWith("HtmlEncodedString"))
            {
                return ((Umbraco.Cms.Core.Strings.HtmlEncodedString)propertyValue).ToHtmlString();
                //return (propertyValue as HtmlString)?.ToHtmlString();
            }


            if (strType == "Umbraco.Core.Udi[]")
            {
                var udiList = propertyValue as Udi[];

                return udiList.Any()
                    ? udiList // should not happen - handled before as list of IPublishedContent or single image
                    : null; // if data type is a single image - but empty - it would also return an empty array which is not convenient on the frontend
            }

            var type = ((object)propertyValue).GetType();
            if (IsNoneComplexType(type) || IsNoneComplexType(GetEnumerableUnderlyingType(type)))
                return propertyValue;

            if (strType == "Newtonsoft.Json.Linq.JObject")
                return (propertyValue as Newtonsoft.Json.Linq.JObject);

            if (propertyValue is IPublishedContent)
                return Resolve(propertyValue);

            if (IsOfAnyType(GetEnumerableUnderlyingType(type), typeof(IPublishedContent)))
                return (propertyValue as IEnumerable<IPublishedContent>)?.Select(s => Resolve(s)).Where(s => s != null);

            if (propertyValue is IPublishedElement)
                return Resolve(propertyValue as IPublishedElement, null);

            if (IsOfAnyType(GetEnumerableUnderlyingType(type), typeof(IPublishedElement)))
                return (propertyValue as IEnumerable<IPublishedElement>).Select(s => ResolveItemFromList(s, null));

            if (IsOfAnyType(GetEnumerableUnderlyingType(type), typeof(Umbraco.Cms.Core.Models.Link)))
            {
                return ResolveLinks(propertyValue);
            }

            var collection = GetEnumerableUnderlyingType(type)?.GetInterfaces()?.Select(s => s.Name);

            if (collection == null)
            {
                collection = new List<string>();
            }

            return $"{strType}::{string.Join(",", collection)}";
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
