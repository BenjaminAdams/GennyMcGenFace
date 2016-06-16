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
        // private static string GetSpaces(depth) = "   ";

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
                str += GetParamValue(member, depth);
            }

            return str;
        }

        //this will help http://stackoverflow.com/questions/6303425/auto-generate-properties-when-creating-object
        private static string ParseObjects(CodeProperty member, int depth)
        {
            if (member.Type.CodeType.Name == "List" || member.Type.CodeType.Name == "ICollection" || member.Type.CodeType.Name == "IList" || member.Type.CodeType.Name == "IEnumerable")
            {
                //list types
                var baseType = member.ProjectItem.ContainingProject.CodeModel.CodeTypeFromFullName(GetBaseTypeFromList(member.Type.AsFullName));
                var typeFullName = string.Format("List<{0}>", baseType.FullName); //and alternative to this could be member.Type.AsFullName

                var objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}},\r\n", GetSpaces(depth + 1), baseType.FullName, IterateMembers(baseType.Members, depth));

                return string.Format("{0}{1} = new {2}() {{\r\n{3}{0}}},\r\n", GetSpaces(depth), member.Name, typeFullName, objAsStr);
            }
            else
            {
                //plain object
                return string.Format("{0}{1} = new {2}() {{\r\n{3}{0}}},\r\n", GetSpaces(depth), member.Name, member.Type.AsFullName, IterateMembers(member.Type.CodeType.Members, depth));
            }
        }

        private static string GetParamValue(CodeProperty member, int depth)
        {
            var rand = new Random();

            if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.Type.AsString == "System.DateTime")
            {
                //DateTime
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                var day = DateTime.Now.Day;
                var dateAsStr = string.Format("new DateTime({0}, {1}, {2})", year, month, day);
                return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), member.Name, dateAsStr);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //defined types/objects we have created
                return ParseObjects(member, depth);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefString)
            {
                //string
                return GetSpaces(depth) + member.Name + " = \"yay\",\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefChar)
            {
                //char
                return GetSpaces(depth) + member.Name + " = 'a',\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefBool)
            {
                //bool
                var val = rand.Next(0, 1) == 1 ? "true" : "false";
                return GetSpaces(depth) + member.Name + " = " + val + ",\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefDecimal || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefDouble || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefFloat || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefInt || member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefLong)
            {
                //numbers (except short)
                var val = rand.Next(0, 999999999);

                return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), member.Name, val);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefShort)
            {
                //short
                var val = rand.Next(0, 9999);
                return string.Format("{0}{1} = {2},\r\n", GetSpaces(depth), member.Name, val);
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                //array
                return GetSpaces(depth) + member.Name + " = \"yay\",\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefByte)
            {
                //byte
                return GetSpaces(depth) + member.Name + " = new Byte(),\r\n";
            }
            else if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefObject)
            {
                //object
                return GetSpaces(depth) + member.Name + " = new Object(),\r\n";
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