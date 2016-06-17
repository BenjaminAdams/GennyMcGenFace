﻿using EnvDTE;
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
        public static string GenerateClass(CodeClass selectedClass)
        {
            var str = string.Format("var obj = new {0}() {{\r\n", selectedClass.FullName);
            str += IterateMembers(selectedClass.Members, 0);
            str += "\r\n};";
            return str;
        }

        private static string IterateMembers(CodeElements members, int depth)
        {
            depth++;
            var str = "";
            foreach (CodeProperty member in members.OfType<CodeProperty>())
            {
                if (CodeDiscoverer.IsValidPublicMember((CodeElement)member) == false) continue;
                str += GetParam(member.Type, member.Name, depth);
            }

            return str;
        }

        //this will help http://stackoverflow.com/questions/6303425/auto-generate-properties-when-creating-object
        private static string ParseObjects(CodeTypeRef member, string paramName, int depth)
        {
            if (member.CodeType.Name == "List" || member.CodeType.Name == "ICollection" || member.CodeType.Name == "IList" || member.CodeType.Name == "IEnumerable")
            {
                //list types
                string objAsStr = string.Empty;
                var baseType = ((CodeProperty)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(GetBaseTypeFromList(member.AsFullName));
                //var baseType = member.ProjectItem.ContainingProject.CodeModel.CodeTypeFromFullName(GetBaseTypeFromList(member.Type.AsFullName));

                var typeFullName = string.Format("List<{0}>", baseType.AsFullName); //and alternative to this could be member.Type.AsFullName

                if (baseType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}},\r\n", GetSpaces(depth + 1), baseType.AsFullName, IterateMembers(baseType.CodeType.Members, depth + 1));
                }
                else
                {
                    objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}},\r\n", GetSpaces(depth + 1), baseType.AsFullName, GetParam(baseType, "", depth + 1));
                }

                return string.Format("{0}{1} = new {2}() {{\r\n{3}{0}}},\r\n", GetSpaces(depth), paramName, typeFullName, objAsStr);
            }
            else
            {
                //plain object
                return string.Format("{0}{1} = new {2}() {{\r\n{3}{0}}},\r\n", GetSpaces(depth), paramName, member.AsFullName, IterateMembers(member.CodeType.Members, depth));
            }
        }

        private static string GetParam(CodeTypeRef member, string paramName, int depth)
        {
            if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.AsString == "System.DateTime")
            {
                //DateTime
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
                return "todo";
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

        private static string GetParamValue(CodeTypeRef member, string paramName, int depth)
        {
            var rand = new Random();

            if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.AsString == "System.DateTime")
            {
                //DateTime
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                var day = DateTime.Now.Day;
                return string.Format("new DateTime({0}, {1}, {2})", year, month, day);
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //defined types/objects we have created
                return ParseObjects(member, paramName, depth);
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefString)
            {
                //string
                return "\"yay\"";
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefChar)
            {
                //char
                return "'a'";
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefBool)
            {
                //bool
                return rand.Next(0, 1) == 1 ? "true" : "false";
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefDecimal || member.TypeKind == vsCMTypeRef.vsCMTypeRefDouble || member.TypeKind == vsCMTypeRef.vsCMTypeRefFloat || member.TypeKind == vsCMTypeRef.vsCMTypeRefInt || member.TypeKind == vsCMTypeRef.vsCMTypeRefLong)
            {
                //numbers (except short)
                return rand.Next(0, 999999999).ToString();
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefShort)
            {
                //short
                return rand.Next(0, 9999).ToString();
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                //array
                //return GetSpaces(depth) + paramName + " = \"yay\",\r\n";
                return "todo";
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