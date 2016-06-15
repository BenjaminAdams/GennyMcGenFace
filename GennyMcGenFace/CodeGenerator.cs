using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.GennyMcGenFace
{
    public static class CodeGenerator
    {
        private static string _leadingSpaces = "   ";

        public static string GenerateClass(CodeClass selectedClass)
        {
            var str = string.Format("var obj = new {0}() {{\r\n", selectedClass.FullName);
            str += IterateMembers(selectedClass.Members);
            str += "\r\n};";
            return str;
        }

        private static string IterateMembers(CodeElements members)
        {
            var str = "";
            foreach (CodeProperty member in members)
            {
                if (IsValidPublicMember((CodeElement)member) == false) continue;

                var fullName = member.FullName;
                var fieldName = member.Name;
                var type = member.Language;
                //str += "   " + member.Name + " = \"yay\",\r\n";
                str += GetParamValue(member);
            }

            return str;
        }

        private static string ParseObjects(CodeProperty member)
        {
            var objAsStr = string.Format("{0}{1} = new {2}() {{\r\n{3}}},", _leadingSpaces, member.Name, member.Type.AsFullName, IterateMembers(member.Type.CodeType.Members));

            if (member.Type.CodeType.Name == "List" || member.Type.CodeType.Name == "IEnumerable" || member.Type.CodeType.Name == "ICollection" || member.Type.CodeType.Name == "IList")
            {
                //list types

                //Something =  new List<>(){
                //    new Something(){
                //        SomeProp= "asd"
                //    }
                //}

                return string.Format("{0} = new List<{1}>() {{\r\n{0}{2}\r\n}},", member.Name, member.Type.AsFullName, objAsStr);
            }
            else
            {
                //plain object
                // var prefix = string.Format("{0} = new {1}() {{\r\n", member.Name, member.Type.AsFullName);
                return objAsStr;
            }
        }

        private static string GetParamValue(CodeProperty member)
        {
            var rand = new Random();

            if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //defined types/objects we have created
                return ParseObjects(member);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefString)
            {
                //string
                return _leadingSpaces + member.Name + " = \"yay\",\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefChar)
            {
                //char
                return _leadingSpaces + member.Name + " = 'a',\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefBool)
            {
                //bool
                var val = rand.Next(0, 1) == 1 ? "true" : "false";
                return _leadingSpaces + member.Name + " = " + val + ",\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefDecimal || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefDouble || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefFloat || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefInt || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefLong)
            {
                //numbers (except short)
                var val = rand.Next(0, 999999999);

                return string.Format("{0}{1} = {2},\r\n", _leadingSpaces, member.Name, val);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefShort)
            {
                //short
                var val = rand.Next(0, 9999);
                return string.Format("{0}{1} = {2},\r\n", _leadingSpaces, member.Name, val);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                //array
                return _leadingSpaces + member.Name + " = \"yay\",\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefByte)
            {
                //byte
                return _leadingSpaces + member.Name + " = new Byte(),\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefObject)
            {
                //object
                return _leadingSpaces + member.Name + " = new Object(),\r\n";
            }
            else
            {
                //skip
                return "";
            }
        }

        public static bool IsValidPublicMember(CodeElement member)
        {
            if (member.Kind == vsCMElement.vsCMElementProperty)
            {
                return ((CodeProperty)member).Access == vsCMAccess.vsCMAccessPublic;
            }
            else
            {
                return false;
            }
        }

        public static bool HasOnePublicMember(CodeClass selectedClass)
        {
            foreach (CodeElement member in selectedClass.Members)
            {
                if (CodeGenerator.IsValidPublicMember(member) == false) continue;

                return true;
            }

            return false;
        }
    }
}