using System;

namespace GennyMcGenFace.Helpers
{
    public static class StringHelper
    {
        public static string RemoveSystemFromStr(this string str)
        {
            if (str.StartsWith("System."))
            {
                str = str.Replace("System.", "");
            }

            return str;
        }
    }
}