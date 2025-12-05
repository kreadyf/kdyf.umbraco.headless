using System.Collections.Generic;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace kdyf.umbraco13.headless.Services
{
    /// <summary>
    /// Equality comparer for IPublishedContent based on ID.
    /// </summary>
    internal class PublishedContentIdEqualityComparer : IEqualityComparer<IPublishedContent>
    {
        public bool Equals(IPublishedContent x, IPublishedContent y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(IPublishedContent obj)
        {
            return obj?.Id.GetHashCode() ?? 0;
        }
    }
}

