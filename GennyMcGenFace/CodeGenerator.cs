using EnvDTE;
using EnvDTE80;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
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
            foreach (CodeProperty member in members.OfType<CodeProperty>())
            {
                if (IsValidPublicMember((CodeElement)member) == false) continue;

                var fullName = member.FullName;
                str += GetParamValue(member);
            }

            return str;
        }

        //this will help http://stackoverflow.com/questions/6303425/auto-generate-properties-when-creating-object
        private static string ParseObjects(CodeProperty member)
        {
            if (member.Type.CodeType.Name == "List" || member.Type.CodeType.Name == "IEnumerable" || member.Type.CodeType.Name == "ICollection" || member.Type.CodeType.Name == "IList")
            {
                //list types
                var typeref = member.Type as EnvDTE.CodeTypeRef;
                var typekind = (EnvDTE.vsCMTypeRef)typeref.TypeKind;

                //var eleType = member.Type.ElementType;
                var asd = member.Type.CodeType.Bases;
                var asd2 = member.Type.CodeType.Members;

                var info = member.InfoLocation;

                var tttttt = GetTypeFromName(member.Type.AsFullName);

                //var membersAsStr = "";
                //var collectionAsStr = "";

                //foreach (var fff in member.Type.CodeType.Members)
                //{
                //    var tmp = (CodeElement)fff;
                //    membersAsStr += tmp.FullName + "\r\n";
                //}

                //foreach (var fff in member.Type.CodeType.Collection)
                //{
                //    var tmp = (CodeElement)fff;
                //    collectionAsStr += tmp.FullName + "\r\n";
                //}

                var name = member.Type.CodeType.Name;
                var namsdfe = member.Type.CodeType.Access;
                var namssdasddfe = member.Type.CodeType.Access;

                //var objAsStr = string.Format("{0}{1} = new {2}() {{\r\n{3}}},", _leadingSpaces, member.Name, member.Type.AsFullName, IterateMembers(member.Type.CodeType.Members));
                var objAsStr = string.Format("{0} new {1}() {{\r\n{0}{0}{2}\r\n{0}}},", _leadingSpaces, member.Type.AsFullName, "SomeProp= \"asd\"");

                return string.Format("{0}{1} = new {2}() {{\r\n{3}\r\n{0}}},\r\n", _leadingSpaces, member.Name, member.Type.AsFullName, objAsStr);
            }
            else
            {
                //plain object
                // var prefix = string.Format("{0} = new {1}() {{\r\n", member.Name, member.Type.AsFullName);
                return string.Format("{0}{1} = new {2}() {{\r\n{3}{0}}},\r\n", _leadingSpaces, member.Name, member.Type.AsFullName, IterateMembers(member.Type.CodeType.Members));
            }
        }

        public static Type GetTypeFromName(string friendlyName)
        {
            var provider = new CSharpCodeProvider();

            var pars = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };

            string code = "public class TypeFullNameGetter"
                        + "{"
                        + "     public override string ToString()"
                        + "     {"
                        + "         return typeof(" + friendlyName + ").FullName;"
                        + "     }"
                        + "}";

            var comp = provider.CompileAssemblyFromSource(pars, new[] { code });

            if (comp.Errors.Count > 0)
                return null;

            object fullNameGetter = comp.CompiledAssembly.CreateInstance("TypeFullNameGetter");
            string fullName = fullNameGetter.ToString();
            return Type.GetType(fullName);
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

        public static bool IsValidPublicMember(CodeElement member)
        {
            var asProp = member as CodeProperty;

            if (asProp != null && member.Kind == vsCMElement.vsCMElementProperty && asProp.Setter != null && asProp.Access == vsCMAccess.vsCMAccessPublic)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool HasOnePublicMember(CodeClass selectedClass)
        {
            foreach (CodeElement member in selectedClass.Members.OfType<CodeElement>())
            {
                if (CodeGenerator.IsValidPublicMember(member) == false) continue;

                return true;
            }

            return false;
        }
    }
}