using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kdyf.umbraco8.headless.Interfaces
{
    public interface IUmbracoHeadlessInterceptorFactory
    {
        IUmbracoHeadlessInterceptor GetInterceptorByDocumentTypeAlias(string documentTypeAlias);
    }
}
