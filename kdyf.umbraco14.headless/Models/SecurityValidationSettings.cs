using kdyf.umbraco9.headless.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace kdyf.umbraco9.headless.Models
{
    public class SecurityValidationSettings
    {
        public Dictionary<int, Guid> PermissionGroups { get; set; }
        public bool IsAuthenticated { get; set; }
        public HashSet<string> PermissionInClaim { get; set; }
        public IContentResolverService<IPublishedContent> ContentResolver { get; set; }
    }
}
