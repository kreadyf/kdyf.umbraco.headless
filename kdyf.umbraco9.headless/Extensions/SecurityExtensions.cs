using kdyf.umbraco9.headless.Constants;
using kdyf.umbraco9.headless.Interfaces;
using kdyf.umbraco9.headless.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace kdyf.umbraco9.headless.Extensions
{
    public static class SecurityExtensions
    {
        public static IPublishedContent ValidateMemberGroups(this IPublishedContent content,
            SecurityValidationSettings settings)
        {
            if (content == null) return content;

            IPublishedContent result = null;

            var properties = settings.ContentResolver.Resolve(content, new string[] { PropertyContants.PermissionGroups, PropertyContants.RequiresAuthentication });

            bool requiresAuthentication = false;
            bool requiresGroup = false;
            bool userInGroup = false;


            foreach (var item in properties)
            {
                string propertyType = item.Key.ToLower();
                object propertyValue = item.Value;

                if (propertyType == PropertyContants.PermissionGroups && !String.IsNullOrEmpty(propertyValue.ToString()))
                {
                    if (propertyValue == null) break; else requiresGroup = true;

                    var ids = (propertyValue.ToString()).Split(',');
                    foreach (var id in ids)
                    {
                        if (Int32.TryParse(id, out var idInt))
                        {
                            if (settings.PermissionGroups.TryGetValue(idInt, out var memberGroupGuid))
                            {
                                userInGroup = settings.PermissionInClaim.Contains(memberGroupGuid.ToString().ToUpper());
                                if (!userInGroup) break;
                            }
                        }
                        else
                        {
                            userInGroup = false;
                            break;
                        }
                    }


                }
                else if (propertyType == PropertyContants.RequiresAuthentication)
                    requiresAuthentication = (bool)propertyValue;
            }

            if ((requiresGroup && userInGroup)
                || (!requiresGroup && !requiresAuthentication)
                || (!requiresGroup && requiresAuthentication && settings.IsAuthenticated))
                result = content;

            return result;
        }
    }
}
