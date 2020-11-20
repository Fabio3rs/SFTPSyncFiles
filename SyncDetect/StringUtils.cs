using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncDetect
{
    static class StringUtils
    {
        // https://stackoverflow.com/questions/5284591/how-to-remove-a-suffix-from-end-of-string
        public static string RemoveFromEnd(this string s, string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }
            else
            {
                return s;
            }
        }
    }
}
