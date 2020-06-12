namespace kdyf.umbraco8.headless.Models
{
    public class NavigationTreeResolverSettings
    {
        public int Depth { get; set; }
        public int ContentDepth { get; set; }

        public string [] ContentToIncludeInMetaProperties { get; set; }
    }
}
