using EnvDTE;
using GennyMcGenFace.Helpers;
using System;
using System.Linq;

namespace GennyMcGenFace
{
    public static class CodeGenerator
    {
        public static string GenerateClass(CodeClass selectedClass)
        {
            var str = string.Format("var obj = new {0}() {{\r\n", selectedClass.FullName);
            str += IterateMembers(selectedClass.Members, 0);
            str += "};";
            return str;
        }

        private static string IterateMembers(CodeElements members, int depth)
        {
            depth++;
            var str = "";
            foreach (CodeProperty member in members.OfType<CodeProperty>())
            {
                try
                {
                    if (CodeDiscoverer.IsValidPublicMember((CodeElement)member) == false) continue;

                    str += GetParam(member.Type, member.Name, depth);
                }
                catch (Exception ex)
                {
                    //ignore silently
                }
            }

            return str;
        }

        private static string GetParam(CodeTypeRef member, string paramName, int depth)
        {
            try
            {
                member = RemoveNullable(member);

                if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.AsString == "System.DateTime")
                {
                    //DateTime
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.AsString == "System.Guid")
                {
                    //Guid
                    return string.Format("{0}{1} = new Guid(\"{2}\"),\r\n", GetSpaces(depth), paramName, Guid.NewGuid());
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.CodeType != null && member.CodeType.Members != null && member.CodeType.Members.Count > 0 && member.CodeType.Kind == vsCMElement.vsCMElementEnum)
                {
                    //Enums
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    //defined types/objects we have created
                    return ParseObjects(member, paramName, depth);
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefString)
                {
                    //string
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefChar)
                {
                    //char
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefBool)
                {
                    //bool
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefDecimal || member.TypeKind == vsCMTypeRef.vsCMTypeRefDouble || member.TypeKind == vsCMTypeRef.vsCMTypeRefFloat || member.TypeKind == vsCMTypeRef.vsCMTypeRefInt || member.TypeKind == vsCMTypeRef.vsCMTypeRefLong)
                {
                    //numbers (except short)
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefShort)
                {
                    //short
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
                {
                    //array
                    return GetArrayParam(member, paramName, depth);
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefByte)
                {
                    //byte
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefObject)
                {
                    //object
                    return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), paramName, GetParamValue(member, paramName, depth));
                }
                else
                {
                    //skip
                    return "";
                }
            }
            catch (Exception ex)
            {
                return string.Format("{0}//{1} = failed\r\n", GetSpaces(depth), paramName);
            }
        }

        private static string GetParamValue(CodeTypeRef member, string paramName, int depth)
        {
            if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.AsString == "System.DateTime")
            {
                //DateTime
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                var day = DateTime.Now.Day;
                return string.Format("new DateTime({0}, {1}, {2})", year, month, day);
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.CodeType != null && member.CodeType.Members != null && member.CodeType.Members.Count > 0 && member.CodeType.Kind == vsCMElement.vsCMElementEnum)
            {
                //Enums
                return member.CodeType.Members.Item(1).FullName;
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //defined types/objects we have created
                return ParseObjects(member, paramName, depth);
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefString)
            {
                //string
                return string.Format("\"{0}\"", Words.Gen(2));
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefChar)
            {
                //char
                var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                return "'" + chars[StaticRandom.Instance.Next(0, chars.Length)] + "'";
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefBool)
            {
                //bool
                return StaticRandom.Instance.Next(0, 1) == 1 ? "true" : "false";
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefDecimal || member.TypeKind == vsCMTypeRef.vsCMTypeRefDouble || member.TypeKind == vsCMTypeRef.vsCMTypeRefFloat || member.TypeKind == vsCMTypeRef.vsCMTypeRefInt || member.TypeKind == vsCMTypeRef.vsCMTypeRefLong)
            {
                //numbers (except short)
                return StaticRandom.Instance.Next(0, 999999999).ToString();
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefShort)
            {
                //short
                return StaticRandom.Instance.Next(0, 9999).ToString();
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                //array
                return "it should not get here";
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefByte)
            {
                //byte
                return "new Byte()";
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefObject)
            {
                //object
                return "new Object()";
            }
            else
            {
                //skip
                return "";
            }
        }

        //this will help http://stackoverflow.com/questions/6303425/auto-generate-properties-when-creating-object
        private static string ParseObjects(CodeTypeRef member, string paramName, int depth)
        {
            if (member.CodeType.Name == "List" || member.CodeType.Name == "ICollection" || member.CodeType.Name == "IList" || member.CodeType.Name == "IEnumerable")
            {
                //list types
                return GetListParam(member, paramName, depth);
            }
            else
            {
                //plain object
                return string.Format("{0}{1} = new {2}() {{\r\n{3}{0}}},\r\n", GetSpaces(depth), paramName, member.AsFullName, IterateMembers(member.CodeType.Members, depth));
            }
        }

        //list logic
        private static string GetListParam(CodeTypeRef member, string paramName, int depth)
        {
            var baseType = ((CodeProperty)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(GetBaseTypeFromList(member.AsFullName));
            if (baseType == null) return string.Empty;

            if (baseType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //typed List
                var objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}},\r\n", GetSpaces(depth + 1), baseType.AsFullName, IterateMembers(baseType.CodeType.Members, depth + 1));
                return string.Format("{0}{1} = new List<{2}>() {{\r\n{3}{0}}},\r\n", GetSpaces(depth), paramName, baseType.AsFullName, objAsStr);
            }
            else
            {
                //generic list, such as string/int
                // var ListString = new List<System.String>() { "yay" };
                // var ListAry = new String[] { "yay" };
                return string.Format("{0}{1} = new List<{2}>() {{ {3} }},\r\n", GetSpaces(depth), paramName, RemoveSystemFromStr(baseType.AsFullName), GetParamValue(baseType, "", depth + 1));
            }
        }

        //array logic
        private static string GetArrayParam(CodeTypeRef member, string paramName, int depth)
        {
            var baseType = ((CodeProperty)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(GetBaseTypeFromArray(member.AsString));
            if (baseType == null) return string.Empty;

            var typeFullName = string.Format("{0}[]", RemoveSystemFromStr(baseType.AsFullName));

            if (baseType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //typed Array
                var objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}},\r\n", GetSpaces(depth + 1), baseType.AsFullName, IterateMembers(baseType.CodeType.Members, depth + 1));
                return string.Format("{0}{1} = new {2} {{\r\n{3}{0}}},\r\n", GetSpaces(depth), paramName, typeFullName, objAsStr);
            }
            else
            {
                //generic array, such as string/int
                // var ListString = new List<System.String>() { "yay" };
                // var ListAry = new String[] { "yay" };
                return string.Format("{0}{1} = new {2} {{ {3} }},\r\n", GetSpaces(depth), paramName, typeFullName, GetParamValue(baseType, "", depth + 1));
            }
        }

        private static CodeTypeRef RemoveNullable(CodeTypeRef member)
        {
            try
            {
                if (member.CodeType != null && member.CodeType.Name == "Nullable")
                {
                    return ((CodeProperty)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(RemoveNullableStr(member.AsFullName));
                }
            }
            catch { }

            return member;
        }

        //super ugly hack to get the base type that the list is on.  Not sure how else to do it
        private static string GetBaseTypeFromList(string fullName)
        {
            fullName = fullName.Replace("System.Collections.Generic.List<", "");
            fullName = fullName.Replace("System.Collections.Generic.IEnumerable<", "");
            fullName = fullName.Replace("System.Collections.Generic.IList<", "");
            fullName = fullName.Replace("System.Collections.Generic.ICollection<", "");
            fullName = fullName.Replace(">", "");
            return fullName;
        }

        private static string RemoveNullableStr(string fullname)
        {
            fullname = fullname.Replace("System.Nullable<", "");
            return fullname.Replace(">", "");
        }

        //super ugly hack to get the base type that the list is on.  Not sure how else to do it
        private static string GetBaseTypeFromArray(string fullName)
        {
            return fullName.Replace("[]", "");
        }

        private static string RemoveSystemFromStr(string str)
        {
            if (str.StartsWith("System."))
            {
                str = str.Replace("System.", "");
            }

            return str;
        }

        private static string GetSpaces(int depth)
        {
            var spaces = "";
            for (var i = 0; i < depth; i++)
            {
                spaces += "   ";
            }

            return spaces;
        }
    }
}