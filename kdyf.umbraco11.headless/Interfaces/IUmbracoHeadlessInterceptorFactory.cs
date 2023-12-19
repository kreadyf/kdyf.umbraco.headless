namespace kdyf.umbraco9.headless.Interfaces
{
    public interface IUmbracoHeadlessInterceptorFactory
    {
        IUmbracoHeadlessInterceptor GetInterceptorByDocumentTypeAlias(string documentTypeAlias);
    }
}
