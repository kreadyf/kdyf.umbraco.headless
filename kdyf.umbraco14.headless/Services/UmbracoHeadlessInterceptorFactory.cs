using kdyf.umbraco9.headless.Interfaces;
using kdyf.umbraco9.headless.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace kdyf.umbraco9.headless.Services
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
