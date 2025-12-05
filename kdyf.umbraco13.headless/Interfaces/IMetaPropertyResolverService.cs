namespace kdyf.umbraco9.headless.Interfaces
{
    public interface IMetaPropertyResolverService<in TSourceContentType>
    {
        dynamic Resolve(TSourceContentType content);
    }
}
