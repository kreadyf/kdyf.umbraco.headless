using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kdyf.umbraco8.headless.Interfaces;

namespace kdyf.umbraco8.headless.Services
{
    public class UmbracoHeadlessInterceptorFactory : IUmbracoHeadlessInterceptorFactory
    {
        private readonly Dictionary<string, IUmbracoHeadlessInterceptor> _interceptors;


        public UmbracoHeadlessInterceptorFactory(Dictionary<string, IUmbracoHeadlessInterceptor> interceptors)
        {
            _interceptors = interceptors;
        }

        public IUmbracoHeadlessInterceptor GetInterceptorByDocumentTypeAlias(string documentTypeAlias)
        {
            if (_interceptors.ContainsKey(documentTypeAlias))
            {
                return _interceptors[documentTypeAlias];
            }

            return null;
        }
    }
}
