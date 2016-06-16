using EnvDTE;
using EnvDTE80;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            foreach (CodeProperty member in members.OfType<CodeProperty>())
            {
                if (CodeDiscoverer.IsValidPublicMember((CodeElement)member) == false) continue;

                //var fullName = member.FullName;
                str += GetParamValue(member);
            }

            return str;
        }

        //this will help http://stackoverflow.com/questions/6303425/auto-generate-properties-when-creating-object
        private static string ParseObjects(CodeProperty member)
        {
            if (member.Type.CodeType.Name == "List" || member.Type.CodeType.Name == "ICollection" || member.Type.CodeType.Name == "IList" || member.Type.CodeType.Name == "IEnumerable")
            {
                //list types

                var typeFullname = member.Type.AsFullName;
                var baseType = member.ProjectItem.ContainingProject.CodeModel.CodeTypeFromFullName(GetBaseTypeFromList(member.Type.AsFullName));

                if (member.Type.CodeType.Name == "IEnumerable")
                {
                    //we have to declare IEumerable's as a list if we want to populate values
                    typeFullname = string.Format("System.Collections.Generic.List<{0}>", baseType.FullName);
                }

                //var objAsStr = string.Format("{0}{1} = new {2}() {{\r\n{3}}},", _leadingSpaces, member.Name, member.Type.AsFullName, IterateMembers(member.Type.CodeType.Members));
                var objAsStr = string.Format("{0} new {1}() {{\r\n{0}{2}\r\n{0}}},", _leadingSpaces, baseType.FullName, IterateMembers(baseType.Members));

                return string.Format("{0}{1} = new {2}() {{\r\n{3}\r\n{0}}},\r\n", _leadingSpaces, member.Name, typeFullname, objAsStr);
            }
            else
            {
                //plain object
                // var prefix = string.Format("{0} = new {1}() {{\r\n", member.Name, member.Type.AsFullName);
                return string.Format("{0}{1} = new {2}() {{\r\n{3}{0}}},\r\n", _leadingSpaces, member.Name, member.Type.AsFullName, IterateMembers(member.Type.CodeType.Members));
            }
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

        private static string GetParamValue(CodeProperty member)
        {
            var rand = new Random();

            if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.Type.AsString == "System.DateTime")
            {
                //DateTime
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                var day = DateTime.Now.Day;
                var dateAsStr = string.Format("new DateTime({0}, {1}, {2})", year, month, day);
                return string.Format("{0}{1} = {2},\r\n", _leadingSpaces, member.Name, dateAsStr);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
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
    }
}