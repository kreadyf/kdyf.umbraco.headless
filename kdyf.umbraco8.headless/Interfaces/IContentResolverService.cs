using Umbraco.Core.Models;

namespace kdyf.umbraco8.headless.Interfaces
{
    public interface IContentResolverService<in TSourceContentType>
    {
        dynamic Resolve(TSourceContentType content, string [] aliases);
    }
}
