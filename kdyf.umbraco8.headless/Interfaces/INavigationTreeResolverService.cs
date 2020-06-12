using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kdyf.umbraco8.headless.Interfaces
{
    public interface INavigationTreeResolverService<in TSourceContent, in TOptions>
    {
        IEnumerable<dynamic> Resolve(TSourceContent content, TOptions options);
    }

}
