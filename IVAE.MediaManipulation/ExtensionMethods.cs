using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public static class ExtensionMethods
  {
    public static object GetValueAndRemove(this Dictionary<string, object> dict, string key)
    {
      if (dict.ContainsKey(key))
      {
        object value = dict[key];
        dict.Remove(key);
        return value;
      }

      return null;
    }
  }
}
