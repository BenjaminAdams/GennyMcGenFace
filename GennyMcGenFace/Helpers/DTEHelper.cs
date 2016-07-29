namespace GennyMcGenFace.Helpers
{
    public static class DTEHelper
    {
        public static string GetBaseType(string fullName)
        {
            fullName = fullName.Replace("System.Collections.Generic.List<", "");
            fullName = fullName.Replace("System.Collections.Generic.IEnumerable<", "");
            fullName = fullName.Replace("System.Collections.Generic.IList<", "");
            fullName = fullName.Replace("System.Collections.Generic.ICollection<", "");
            fullName = fullName.Replace("System.Threading.Tasks.Task<", "");

            fullName = fullName.Replace(">", "");
            return fullName;
        }

        public static string RemoveTaskFromString(string fullName)
        {
            if (!fullName.Contains("System.Threading.Tasks.Task<")) return fullName;

            fullName = fullName.Replace("System.Threading.Tasks.Task<", "");
            fullName = fullName.Remove(fullName.LastIndexOf(">"), 1);

            return fullName;
        }

        public static string RemoveNullableStr(string fullName)
        {
            if (!fullName.Contains("System.Nullable<")) return fullName;

            fullName = fullName.Replace("System.Nullable<", "");
            return fullName.Remove(fullName.LastIndexOf(">"), 1);
        }

        //super ugly hack to get the base type that the list is on.  Not sure how else to do it
        public static string GetBaseTypeFromArray(string fullName)
        {
            return fullName.Replace("[]", "");
        }

        public static string GenPrivateClassNameAtTop(string className)
        {
            return "_" + className.FirstCharacterToLower();
        }
    }
}