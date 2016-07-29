using EnvDTE;
using EnvDTE80;
using GennyMcGenFace.Helpers;
using GennyMcGenFace.Models;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GennyMcGenFace.Parsers
{
    public class ClassGenerator
    {
        private static GenOptions _opts;
        private UnitTestParts _parts;
        private DTE2 _dte;

        private static readonly Dictionary<string, Type> _knownPrimitiveTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) {
            { "string", typeof( string ) },
            { "int", typeof( int ) },
            { "long", typeof( long ) },
            { "short", typeof( short ) },
            { "byte", typeof( byte ) },
            { "uint", typeof( uint ) },
            { "ulong", typeof( ulong ) },
            { "ushort", typeof( ushort ) },
            { "sbyte", typeof( sbyte ) },
            { "float", typeof( float ) },
            { "double", typeof( double ) },
            { "decimal", typeof( decimal ) },
        };

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
            member = RemoveNullable(member);

            // if (member.CodeType == null) return "Error";
            if (member == null) return "Error";

            AddNameSpace(member);

            if (member.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                //array
                return GetArrayParamValue(member, depth);
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.AsString == "System.DateTime")
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
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && member.AsString == "System.Guid")
            {
                //Guid
                return string.Format("new Guid(\"{0}\")", Guid.NewGuid());
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && IsCodeTypeAList(member.CodeType.Name))
            {
                return GetListParamValue(member, depth);
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                var paramsInConstructorStr = string.Empty;
                if (member.CodeType.Members != null)
                {
                    var constructor = member.CodeType.Members.OfType<CodeFunction>().FirstOrDefault(x => x.FunctionKind == vsCMFunction.vsCMFunctionConstructor);
                    if (constructor != null && constructor.Kind == vsCMElement.vsCMElementFunction)
                    {
                        paramsInConstructorStr = GenerateFunctionParamValues(constructor);
                    }
                }

                var includedNewLineInParams = string.Empty;
                var initializerStr = IterateMembers(member.CodeType, depth);
                if (string.IsNullOrWhiteSpace(initializerStr) == false)
                {
                    includedNewLineInParams = "\r\n";
                    initializerStr += Spacing.Get(depth);
                }

                //defined types/objects we have created
                return string.Format("new {0}({2}) {{{3}{1}}}", member.AsString, initializerStr, paramsInConstructorStr, includedNewLineInParams);
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefString)
            {
                //string
                return string.Format("\"{0}\"", Words.Gen(_opts.WordsInStrings));
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
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefDecimal ||
                     member.TypeKind == vsCMTypeRef.vsCMTypeRefDouble ||
                     member.TypeKind == vsCMTypeRef.vsCMTypeRefFloat || member.TypeKind == vsCMTypeRef.vsCMTypeRefInt ||
                     member.TypeKind == vsCMTypeRef.vsCMTypeRefLong || member.AsString == "uint" || member.AsString == "ulong")
            {
                //numbers (except short)
                if (_opts.IntLength == 0) return "0";
                return StaticRandom.Instance.Next(_opts.GetMaxIntLength()).ToString();
            }
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefShort)
            {
                //short
                if (_opts.IntLength == 0) return "0";
                var maxRnd = _opts.IntLength > 4 ? 9999 : _opts.GetMaxIntLength();
                return StaticRandom.Instance.Next(maxRnd).ToString();
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
            else if (member.AsString == "sbyte")
            {
                //sbyte
                return StaticRandom.Instance.Next(-128, 127).ToString();
            }
            else if (member.AsString == "ushort")  //no, YOU'RE SHORT!
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

        public string GenerateFunctionParamValues(CodeFunction member)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var paramsStr = "";

            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                try
                {
                    if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
                    {
                        AddNameSpace(param.Type.CodeType.Namespace);
                    }

                    if (param.Type.CodeType.Bases.Cast<CodeClass>().Any(item => item.FullName == "System.Data.Entity.DbContext"))
                    {
                        //Genny would create a huge class of a mock database that wouldnt even work!
                        paramsStr += string.Format("new {0}(), ", param.Type.CodeType.Name);
                    }
                    else if (param.Type != null && param.Type.CodeType.Kind == vsCMElement.vsCMElementInterface)
                    {
                        //generate interfaces
                        paramsStr += GenerateInterface(param) + ", ";
                    }
                    else if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType && (param.Type.AsString != "System.Guid" && param.Type.AsString != "System.DateTime" && param.Type.CodeType.Kind != vsCMElement.vsCMElementEnum))
                    {
                        //functions
                        //if the param is a CodeClass we can create an input object for it

                        var functionName = GenerateFunctionParamForClassInput(param.Type.CodeType.Name, param.Type.AsFullName, param.Type);
                        // paramsStr += string.Format("{0}Input, ", param.Name);
                        paramsStr += string.Format("{0}(), ", functionName);
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

        public string GenerateFunctionParamForClassInput(string name, string fullName, CodeTypeRef codeTypeRef)
        {
            codeTypeRef = StripGenerics(codeTypeRef);

            fullName = codeTypeRef.AsFullName.Replace("?", "");
            name = codeTypeRef.CodeType.Name.Replace("?", "");

            var fullNameToUseAsReturnType = fullName;

            if (ClassGenerator.IsCodeTypeAList(name))
            {
                var baseType = TryToGuessGenericArgument(codeTypeRef);
                // fullNameToUseAsReturnType = string.Format("{0}<{1}>", name, codeTypeRef.CodeType.Name);
                name = baseType == null ? "//Couldnt get list type name" : baseType.CodeType.Name + "List";
            }
            //else if (name == "Task")
            //{
            //    var baseType = TryToGuessGenericArgument(codeTypeRef);
            //    name = baseType == null ? "//Couldnt get type from Task" : baseType.CodeType.Name;
            //    fullNameToUseAsReturnType = DTEHelper.RemoveTaskFromString(fullNameToUseAsReturnType);
            //}

            var exists = _parts.ParamsGenerated.FirstOrDefault(x => x.FullName == fullName);
            if (exists != null) return exists.GetFunctionName; //do not add a 2nd one

            var functionName = string.Format("Get{0}", name);

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

            var gen = string.Format(@"
        private static {0} {1}() {{
            return {2};
        }}
        ", fullNameToUseAsReturnType, functionName, innerCode);

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
            var baseType = TryToGuessGenericArgument(member);
            if (baseType == null) return string.Empty;

            if (baseType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
            {
                //typed List
                var objAsStr = string.Format("{0}new {1}() {{\r\n{2}{0}}}\r\n", Spacing.Get(depth + 1), baseType.AsFullName, IterateMembers(baseType.CodeType, depth + 1));
                return string.Format("new List<{1}>() {{\r\n{2}{0}}}", Spacing.Get(depth), baseType.AsFullName, objAsStr);
            }
            else
            {
                //generic list, such as string/int
                // var ListString = new List<System.String>() { "yay" };
                return string.Format("new List<{0}>() {{ {1} }}", baseType.AsFullName.RemoveSystemFromStr(), GetParamValue(baseType, "", depth + 1));
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

                    var codeType = projCodeModel.CodeTypeFromFullName(TryToGuessFullName(typeNameAsInCode));

                    if (codeType != null) return projCodeModel.CreateCodeTypeRef(codeType);
                    return member;
                }
                catch (COMException ex)
                {
                    var found = CheckForTypeInOtherPlaces(typeNameAsInCode);
                    if (found != null) return found;
                }

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

                    var codeType = projCodeModel.CodeTypeFromFullName(TryToGuessFullName(typeNameAsInCode));

                    if (codeType != null) return projCodeModel.CreateCodeTypeRef(codeType);
                    return member;
                }
                catch (COMException ex)
                {
                    var found = CheckForTypeInOtherPlaces(typeNameAsInCode);
                    if (found != null) return found;
                }

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
                        var codeType = activeProj.CodeModel.CodeTypeFromFullName(TryToGuessFullName(typeNameAsInCode));
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
            foreach (Project proj in _dte.Solution.Projects)
            {
                try
                {
                    var codeType = proj.CodeModel.CodeTypeFromFullName(TryToGuessFullName(typeNameAsInCode));

                    if (codeType != null)
                    {
                        return proj.CodeModel.CreateCodeTypeRef(codeType);
                    }
                }
                catch { }
            }
            return null;
        }

        private static string TryToGuessFullName(string typeName)
        {
            Type primitiveType;
            if (_knownPrimitiveTypes.TryGetValue(typeName, out primitiveType)) return primitiveType.FullName;
            else return typeName;
        }

        private static bool IsPrimitive(CodeTypeRef codeTypeRef)
        {
            if (codeTypeRef.TypeKind != vsCMTypeRef.vsCMTypeRefOther && codeTypeRef.TypeKind != vsCMTypeRef.vsCMTypeRefCodeType)
                return true;

            if (codeTypeRef.AsString.EndsWith("DateTime", StringComparison.Ordinal))
                return true;

            return false;
        }

        private CodeTypeRef RemoveNullable(CodeTypeRef member)
        {
            try
            {
                if (member.CodeType != null && member.CodeType.Name == "Nullable")
                {
                    return TryToGuessGenericArgument(member);
                }
            }
            catch (Exception ex)
            {
                //
            }

            return member;
        }
    }
}