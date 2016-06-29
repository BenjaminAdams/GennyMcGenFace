namespace GennyMcGenFace.Helpers
{
    public static class DTEHelper
    {
        public static string GetBaseTypeFromList(string fullName)
        {
            fullName = fullName.Replace("System.Collections.Generic.List<", "");
            fullName = fullName.Replace("System.Collections.Generic.IEnumerable<", "");
            fullName = fullName.Replace("System.Collections.Generic.IList<", "");
            fullName = fullName.Replace("System.Collections.Generic.ICollection<", "");
            fullName = fullName.Replace(">", "");
            return fullName;
        }

        public static string RemoveNullableStr(string fullname)
        {
            fullname = fullname.Replace("System.Nullable<", "");
            return fullname.Replace(">", "");
        }

        //super ugly hack to get the base type that the list is on.  Not sure how else to do it
        public static string GetBaseTypeFromArray(string fullName)
        {
            return fullName.Replace("[]", "");
        }
    }
}