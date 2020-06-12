using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Xml.XPath;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Owin;
using kdyf.umbraco8.headless.Interfaces;
using kdyf.umbraco8.headless.Models;
using kdyf.umbraco8.headless.Services;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using File = System.IO.File;

namespace kdyf.umbraco8.headless
{
    // check https://github.com/umbraco/Umbraco-CMS/blob/8f32fce7818a556d4879e66851dbe91221c63fdc/src/Umbraco.Web/UmbracoDefaultOwinStartup.cs#L69
    public partial class Startup : UmbracoDefaultOwinStartup
    {
        public override void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);

            app.UseHeadlessUmbacoCms();

            base.Configuration(app);
        }
    }
}
