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

        public static string ReplaceLastOccurrence(this string str, string find, string replace)
        {
            var place = str.LastIndexOf(find);

            if (place == -1) return str;

            var result = str.Remove(place, find.Length).Insert(place, replace);
            return result;
        }
    }
}