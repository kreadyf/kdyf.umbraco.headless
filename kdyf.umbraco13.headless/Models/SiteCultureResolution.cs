namespace kdyf.umbraco13.headless.Models
{
    /// <summary>
    /// Represents the resolved site culture information for a request.
    /// </summary>
    public class SiteCultureResolution
    {
        /// <summary>
        /// The root node ID for the site.
        /// </summary>
        public int RootNodeId { get; set; }

        /// <summary>
        /// The effective culture to use for content resolution (root-node's default culture).
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// The relative path within the root node (with root path and culture segment stripped).
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// The root node content item.
        /// </summary>
        public Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent RootNode { get; set; }
    }
}

