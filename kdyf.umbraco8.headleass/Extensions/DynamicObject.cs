using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Dynamitey;
using Newtonsoft.Json;

namespace kdyf.umbraco8.headless.Extensions
{
    public static class DynamicObject
    {
        public static dynamic Merge(params dynamic [] objs)
        {
            IDictionary<string, object> result = new ExpandoObject();
            
            foreach (var obj in objs)
            {
                foreach (var propName in Dynamic.GetMemberNames(obj))
                    if (!result.ContainsKey(propName))
                        result[propName] = Dynamic.InvokeGet(obj, propName);
            }

            return result;
        }
    }
}
