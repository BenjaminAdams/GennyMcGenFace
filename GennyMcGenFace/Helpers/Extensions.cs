using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.Helpers
{
    public static class Extensions
    {
        public static string FirstCharacterToLower(this string str)
        {
            if (String.IsNullOrEmpty(str) || Char.IsLower(str, 0))
                return str;

            return Char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string FirstCharacterToUpper(this string str)
        {
            if (String.IsNullOrEmpty(str) || Char.IsUpper(str, 0))
                return str;

            return Char.ToUpperInvariant(str[0]) + str.Substring(1);
        }

        public static void AddIfNotExists(this List<string> lst, string str)
        {
            if (lst.Contains(str) == true) return;
            lst.Add(str);
        }

        public static void AddIfNotExists(this List<CodeInterface> lst, CodeInterface codeInterface)
        {
            if (codeInterface == null || codeInterface.Name == null || lst == null) return;
            if (lst.Any(x => x.FullName == codeInterface.FullName)) return;
            lst.Add(codeInterface);
        }
    }
}