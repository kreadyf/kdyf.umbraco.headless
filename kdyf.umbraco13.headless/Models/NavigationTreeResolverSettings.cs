using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kdyf.umbraco9.headless.Models
{
    public class NavigationTreeResolverSettings
    {
        public int Depth { get; set; }
        public int ContentDepth { get; set; }

        public string[] ContentToIncludeInMetaProperties { get; set; }
    }
}
