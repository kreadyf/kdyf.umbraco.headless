using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace kdyf.umbraco9.headless.Routing
{
    public class HeadlessRouteConstraint : IRouteConstraint
    {
        private static readonly List<Regex> _regex = new List<Regex> {
            new(@"^.*.[\.]axd[\/].*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            new(@"^(lib[\/].*)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            new(@"^(umbraco[\/].*)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            new(@"^(media[\/].*)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            new(@"^(umbraco_client[\/].*)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            new(@"^(App_Plugins[\/].*)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            new(@"^(favicon[\.]ico)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            new(@"^(sb[\/]umbraco.*)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),            
        };

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            
            if (!values.TryGetValue(routeKey, out var routeValue))
            {
                return false;
            }

            var routeValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture);

            if (routeValueString is null)
            {
                return false;
            }

            bool isMatch = false;

            foreach (var item in _regex)
            {
                isMatch = isMatch || item.IsMatch(routeValueString + "/");
            }

            return !isMatch;
        }
    }
}
