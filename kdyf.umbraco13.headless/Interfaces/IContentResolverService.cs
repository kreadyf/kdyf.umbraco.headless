namespace kdyf.umbraco9.headless.Interfaces
{
    public interface IContentResolverService<in TSourceContentType>
    {
        dynamic Resolve(TSourceContentType content, string[] aliases);
    }
}
