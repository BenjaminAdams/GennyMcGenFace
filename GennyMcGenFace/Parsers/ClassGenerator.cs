using EnvDTE;
using EnvDTE80;
using GennyMcGenFace.Helpers;
using GennyMcGenFace.Models;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GennyMcGenFace.Parsers
{
    public class ClassGenerator
    {
        private static GenOptions _opts;
        private UnitTestParts _parts;
        private DTE2 _dte;

        public ClassGenerator(UnitTestParts parts, GenOptions opts, DTE2 dte)
        {
            _opts = opts;
            _parts = parts ?? new UnitTestParts();
            _dte = dte;
        }

        public string GenerateClassStr(CodeClass selectedClass, int depth = 0)
        {
            var str = string.Format("var obj = new {0}() {{\r\n", selectedClass.FullName);
            str += IterateMembers((CodeType)selectedClass, depth);
            str += Spacing.Get(depth) + "};";
            return str;
        }

        public string IterateMembers(CodeType selectedClass, int depth)
        {
            depth++;
            var str = "";
            foreach (CodeProperty member in selectedClass.Members.OfType<CodeProperty>())
            {
                try
                {
                    if (CodeDiscoverer.IsValidPublicProperty((CodeElement)member) == false) continue;

                    str += GetParam(member.Type, member.Name, depth);
                }
                catch (Exception ex)
                {
                    //ignore silently
                }
            }

            str = str.ReplaceLastOccurrence(",", "");
            return str;
        }

        public void AddNameSpace(CodeTypeRef member)
        {
            try
            {
                if (member != null && member.CodeType != null && member.CodeType.Namespace != null)
                {
                    _parts.NameSpaces.AddIfNotExists(member.CodeType.Namespace.FullName);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void AddNameSpace(CodeNamespace ns)
        {
            try
            {
                if (ns != null)
                {
                    _parts.NameSpaces.AddIfNotExists(ns.FullName);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public string GetParam(CodeTypeRef member, string paramName, int depth)
        {
            if (depth > 7) return string.Empty; //prevent ifinite loop

            try
            {
                return string.Format("{0}{1} = {2},\r\n", Spacing.Get(depth), paramName, GetParamValue(member, paramName, depth));
            }
            catch (Exception ex)
            {
                return string.Format("{0}//{1} = failed\r\n", Spacing.Get(depth), paramName);
            }
        }

        public string GetParamValue(CodeTypeRef member, string paramName, int depth)
        {
            var strippedMember = StripGenerics(member);

            // if (member.CodeType == null) return "Error";
            if (member == null) return "Error";

            AddNameSpace(member);

            if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                //array
                return GetArrayParamValue(member, depth);
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && strippedMember.AsString == "System.DateTime")
            {
                //DateTime
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                var day = DateTime.Now.Day;
                return string.Format("new DateTime({0}, {1}, {2})", year, month, day);
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && strippedMember.CodeType != null && strippedMember.CodeType.Members != null && strippedMember.CodeType.Members.Count > 0 && strippedMember.CodeType.Kind == vsCMElement.vsCMElementEnum)
            {
                //Enums
                return strippedMember.CodeType.Members.Item(1).FullName;
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && strippedMember.AsString == "System.Guid")
            {
                //Guid
                return string.Format("new Guid(\"{0}\")", Guid.NewGuid());
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && IsCodeTypeAList(strippedMember.CodeType.Name))
            {
                return GetListParamValue(member, depth);
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                var paramsInConstructorStr = string.Empty;
                if (strippedMember.CodeType.Members != null)
                {
                    //var constructor = member.CodeType.Members.OfType<CodeFunction>().FirstOrDefault(x => x.FunctionKind == vsCMFunction.vsCMFunctionConstructor);
                    var constructor = strippedMember.CodeType.Members.OfType<CodeFunction>().FirstOrDefault(x => x.FunctionKind == vsCMFunction.vsCMFunctionConstructor);
                    if (constructor != null && constructor.Kind == vsCMElement.vsCMElementFunction)
                    {
                        paramsInConstructorStr = GenerateFunctionParamValues(constructor, false);
                    }
                }

                var includedNewLineInParams = string.Empty;

                //var initializerStr = IterateMembers(member.CodeType, depth);
                var initializerStr = IterateMembers(strippedMember.CodeType, depth);
                if (string.IsNullOrWhiteSpace(initializerStr) == false)
                {
                    includedNewLineInParams = "\r\n";
                    initializerStr += Spacing.Get(depth);
                }

                //defined types/objects we have created
                return string.Format("new {0}({2}) {{{3}{1}}}", strippedMember.AsString, initializerStr, paramsInConstructorStr, includedNewLineInParams);
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefString)
            {
                //string
                return string.Format("\"{0}\"", Words.Gen(_opts.WordsInStrings));
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefChar)
            {
                //char
                var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                return "'" + chars[StaticRandom.Instance.Next(0, chars.Length)] + "'";
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefBool)
            {
                //bool
                return StaticRandom.Instance.Next(0, 1) == 1 ? "true" : "false";
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefDecimal ||
                     strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefDouble ||
                     strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefFloat || strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefInt ||
                     strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefLong || strippedMember.AsString == "uint" || strippedMember.AsString == "ulong")
            {
                //numbers (except short)
                if (_opts.IntLength == 0) return "0";
                return StaticRandom.Instance.Next(_opts.GetMaxIntLength()).ToString();
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefShort)
            {
                //short
                if (_opts.IntLength == 0) return "0";
                var maxRnd = _opts.IntLength > 4 ? 9999 : _opts.GetMaxIntLength();
                return StaticRandom.Instance.Next(maxRnd).ToString();
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefByte)
            {
                //byte
                return "new Byte()";
            }
            else if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefObject)
            {
                //object
                return "new Object()";
            }
            else if (strippedMember.AsString == "sbyte")
            {
                //sbyte
                return StaticRandom.Instance.Next(-128, 127).ToString();
            }
            else if (strippedMember.AsString == "ushort")  //no, YOU'RE SHORT!
            {
                //ushort
                return StaticRandom.Instance.Next(65535).ToString();
            }
            else
            {
                //skip
                return "";
            }
        }

        public string GenerateFunctionParamValues(CodeFunction member, bool paramsDefinedB4Input)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var paramsStr = "";
            var paramCount = 0;
            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                try
                {
                    var strippedParam = StripGenerics(param.Type);
                    paramCount++;
                    if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
                    {
                        AddNameSpace(param.Type.CodeType.Namespace);
                    }

                    if (param.Type.CodeType.Bases.Cast<CodeClass>().Any(item => item.FullName == "System.Data.Entity.DbContext"))
                    {
                        //Genny would create a huge class of a mock database that wouldnt even work!
                        paramsStr += string.Format("new {0}(), ", param.Type.CodeType.Name);
                    }
                    else if (strippedParam != null && strippedParam.CodeType.Kind == vsCMElement.vsCMElementInterface)
                    {
                        //generate interfaces
                        paramsStr += GenerateInterface(param) + ", ";
                    }
                    else if (strippedParam != null && strippedParam.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && (strippedParam.AsString != "System.Guid" && strippedParam.AsString != "System.DateTime" && strippedParam.CodeType.Kind != vsCMElement.vsCMElementEnum))
                    {
                        //functions
                        //if the param is a CodeClass we can create an input object for it
                        var functionName = GenerateFunctionParamForClassInput(param.Type);

                        if (paramsDefinedB4Input)
                        {
                            paramsStr += string.Format("param{0}, ", paramCount);
                        }
                        else
                        {
                            paramsStr += string.Format("{0}(), ", functionName);
                        }
                    }
                    else
                    {
                        paramsStr += GetParamValue(param.Type, param.Name, 0) + ", ";
                    }
                }
                catch (Exception ex)
                {
                    paramsStr += string.Format("failed {0}, ", param.Name);
                }
            }

            paramsStr = paramsStr.Trim().TrimEnd(',');
            return paramsStr;
        }

        private string GenerateInterface(CodeParameter param)
        {
            var codeInterface = (CodeInterface)param.Type.CodeType;

            _parts.PrivateClassesAtTop.AddIfNotExists(codeInterface.Name);
            _parts.Interfaces.AddIfNotExists(codeInterface);
            AddNameSpace(param.Type.CodeType.Namespace);

            return DTEHelper.GenPrivateClassNameAtTop(codeInterface.Name);
        }

        public string GenerateFunctionParamForClassInput(CodeTypeRef codeTypeRef)
        {
            var strippedCodeTypeRef = StripGenerics(codeTypeRef);

            var fullName = strippedCodeTypeRef.AsFullName.Replace("?", "");
            var name = strippedCodeTypeRef.CodeType.Name.Replace("?", "");

            if (ClassGenerator.IsCodeTypeAList(name))
            {
                var baseType = TryToGuessGenericArgument(codeTypeRef);
                if (baseType == null) return null;
                name = baseType.CodeType.Name + "List";
                fullName = strippedCodeTypeRef.AsFullName.Replace("?", "");
            }

            var exists = _parts.ParamsGenerated.FirstOrDefault(x => x.FullName == fullName);
            if (exists != null) return exists.GetFunctionName; //do not add a 2nd one

            var functionName = string.Format("Get{0}", name);

            //if (functionName == "GetList")
            //  {
            //       var asdasdsad = 55;
            //  }

            //_parts.ParamsGenerated keeps a list of functions that will get the value of the object we generated
            _parts.ParamsGenerated.Add(new ParamsGenerated() { FullName = fullName, GetFunctionName = functionName });

            string innerCode;
            if (functionName == "GetTask")
            {
                innerCode = "new Task()";
            }
            else
            {
                innerCode = GetParamValue(codeTypeRef, string.Empty, 3);
            }

            if (innerCode.Contains("Task"))
            {
                var asdasd = 444;
            }

            var gen = string.Format(@"
        private static {0} {1}() {{
            return {2};
        }}
        ", fullName, functionName, innerCode);

            _parts.ParamInputs += gen;
            return functionName;
        }

        //this will help http://stackoverflow.com/questions/6303425/auto-generate-properties-when-creating-object
        //private string ParseObjects(CodeTypeRef member, string paramName, int depth)
        //{
        //}

        public static bool IsCodeTypeAList(string name)
        {
            return name == "List" || name == "ICollection" || name == "IList" || name == "IEnumerable";
        }

        //list logic
        private string GetListParamValue(CodeTypeRef member, int depth)
        {
            var strippedMember = TryToGuessGenericArgument(member);
            if (strippedMember == null) return string.Empty;

            if (strippedMember.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //typed List
                var objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}}\r\n", Spacing.Get(depth + 1), strippedMember.AsFullName, IterateMembers(strippedMember.CodeType, depth + 1));
                return string.Format("new List<{1}>() {{\r\n{2}{0}}}", Spacing.Get(depth), strippedMember.AsFullName, objAsStr);
            }
            else
            {
                //generic list, such as string/int
                // var ListString = new List<System.String>() { "yay" };
                return string.Format("new List<{0}>() {{ {1} }}", strippedMember.AsFullName.RemoveSystemFromStr(), GetParamValue(strippedMember, "", depth + 1));
            }
        }

        //array logic
        private string GetArrayParamValue(CodeTypeRef member, int depth)
        {
            //var baseType = ((CodeElement)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.GetBaseTypeFromArray(member.AsString));
            // var baseType = TryToGuessGenericArgument(member);
            var baseType = member.ElementType;

            if (baseType == null) return string.Empty;

            var typeFullName = string.Format("{0}[]", baseType.AsFullName.RemoveSystemFromStr());

            if (baseType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //typed Array
                var objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}}\r\n", Spacing.Get(depth + 1), baseType.AsFullName, IterateMembers(baseType.CodeType, depth + 1));
                return string.Format("new {0} {{\r\n{1}{2}}}", typeFullName, objAsStr, Spacing.Get(depth));
            }
            else
            {
                //generic array, such as string/int
                // var ListAry = new String[] { "yay" };
                return string.Format("new {0} {{ {1} }}", typeFullName, GetParamValue(baseType, "", depth + 1));
            }
        }

        public CodeTypeRef TryToGuessGenericArgument(CodeTypeRef member)
        {
            try
            {
                if (member.AsFullName.Contains("<") == false) return member; //No need to attempt to guess, this is not a generic class

                //todo check if we need to cast to CodeTypeRef2 here
                var codeTypeRef2 = member as CodeTypeRef2;
                if (codeTypeRef2 == null || !codeTypeRef2.IsGeneric) return member;

                // There is no way to extract generic parameter as CodeTypeRef or something similar
                // (see http://social.msdn.microsoft.com/Forums/vstudio/en-US/09504bdc-2b81-405a-a2f7-158fb721ee90/envdte-envdte80-codetyperef2-and-generic-types?forum=vsx)
                // but we can make it work at least for some simple case with the following heuristic:
                //  1) get the argument's local name by parsing the type reference's full text
                //  2) if it's a known primitive (i.e. string, int, etc.), return that
                //  3) otherwise, guess that it's a type from the same namespace and same project,
                //     and use the project CodeModel to retrieve it by full name
                //  4) if CodeModel returns null - well, bad luck, don't have any more guesses

                var typeNameAsInCode = DTEHelper.RemoveTaskFromString(codeTypeRef2.AsFullName);
                // var typeNameAsInCode = codeTypeRef2.AsString.Replace("?", "");
                //typeNameAsInCode = typeNameAsInCode.Split('<', '>').ElementAtOrDefault(1) ?? "";
                typeNameAsInCode = typeNameAsInCode.Split('<', '>').ElementAtOrDefault(1) ?? typeNameAsInCode;

                try
                {
                    CodeModel projCodeModel = ((CodeElement)member.Parent).ProjectItem.ContainingProject.CodeModel;
                    if (projCodeModel == null) return member;

                    var codeType = projCodeModel.CodeTypeFromFullName(typeNameAsInCode);

                    if (codeType != null) return projCodeModel.CreateCodeTypeRef(codeType);
                    return member;
                }
                catch (COMException ex)
                {
                    var found = CheckForTypeInOtherPlaces(typeNameAsInCode);
                    if (found != null) return found;
                }

                if (member.AsFullName.Contains("Task<")) return null; //we failed, might as well throw error
                if (member.AsFullName.Contains("List<")) return null; //we failed, might as well throw error

                return member;
            }
            catch (Exception ex)
            {
                return member;
            }
        }

        public CodeTypeRef StripGenerics(CodeTypeRef member)
        {
            try
            {
                if (member.AsFullName.Contains("<") == false) return member; //No need to attempt to guess, this is not a generic class

                //todo check if we need to cast to CodeTypeRef2 here
                var codeTypeRef2 = member as CodeTypeRef2;
                if (codeTypeRef2 == null || !codeTypeRef2.IsGeneric) return member;

                var typeNameAsInCode = DTEHelper.RemoveTaskFromString(codeTypeRef2.AsFullName);
                typeNameAsInCode = DTEHelper.RemoveNullableStr(typeNameAsInCode);

                try
                {
                    CodeModel projCodeModel = ((CodeElement)member.Parent).ProjectItem.ContainingProject.CodeModel;
                    if (projCodeModel == null) return member;

                    var codeType = projCodeModel.CodeTypeFromFullName(typeNameAsInCode);

                    if (codeType != null) return projCodeModel.CreateCodeTypeRef(codeType);
                    return member;
                }
                catch (COMException ex)
                {
                    var found = CheckForTypeInOtherPlaces(typeNameAsInCode);
                    if (found != null) return found;
                }

                if (member.AsFullName.Contains("Task<")) return null; //we failed, might as well throw error

                return member;
            }
            catch (Exception ex)
            {
                return member;
            }
        }

        public CodeTypeRef CheckForTypeInOtherPlaces(string typeNameAsInCode)
        {
            var found1 = CheckForTypeInActiveProject(typeNameAsInCode);
            if (found1 != null) return found1;
            var found2 = CheckForTypeInAllProjects(typeNameAsInCode);
            if (found2 != null) return found2;
            return null;
        }

        public CodeTypeRef CheckForTypeInActiveProject(string typeNameAsInCode)
        {
            try
            {
                Array activeProjs = _dte.ActiveSolutionProjects as Array;

                if (activeProjs != null && activeProjs.Length > 0)
                {
                    var activeProj = activeProjs.GetValue(0) as Project;
                    if (activeProj != null)
                    {
                        var codeType = activeProj.CodeModel.CodeTypeFromFullName(typeNameAsInCode);
                        if (codeType != null) return activeProj.CodeModel.CreateCodeTypeRef(codeType);
                    }
                }
            }
            catch { }

            return null;
        }

        public CodeTypeRef CheckForTypeInAllProjects(string typeNameAsInCode)
        {
            //lol check in all projects
            foreach (Project projFolders in CodeDiscoverer.Projects())
            {
                try
                {
                    var codeType = projFolders.CodeModel.CodeTypeFromFullName(typeNameAsInCode);

                    if (codeType != null)
                    {
                        return projFolders.CodeModel.CreateCodeTypeRef(codeType);
                    }
                }
                catch { }
            }
            return null;
        }
    }
}