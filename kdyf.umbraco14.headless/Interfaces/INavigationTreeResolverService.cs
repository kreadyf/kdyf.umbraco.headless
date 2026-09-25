using kdyf.umbraco9.headless.Models;
using System.Collections.Generic;

namespace kdyf.umbraco9.headless.Interfaces
{
    public interface INavigationTreeResolverService<in TSourceContent, in TOptions>
    {
        IEnumerable<dynamic> Resolve(TSourceContent content, TOptions options, SecurityValidationSettings securityoptions = null);
    }
}
