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

        private void AddNameSpace(CodeTypeRef member)
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

        public string GetParam(CodeTypeRef member, string paramName, int depth)
        {
            if (depth > 6) return string.Empty; //prevent ifinite loop

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

            AddNameSpace(member);

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
                var constructor = (CodeFunction)member.CodeType.Members.OfType<CodeFunction>().FirstOrDefault(x => x.FunctionKind == vsCMFunction.vsCMFunctionConstructor);
                if (constructor != null && constructor.Kind == vsCMElement.vsCMElementFunction)
                {
                    paramsInConstructorStr = GenerateFunctionParamValues((CodeFunction)constructor);
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
                     member.TypeKind == vsCMTypeRef.vsCMTypeRefLong)
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
            else if (member.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                //array
                return GetArrayParamValue(member, depth);
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

        public string GenerateFunctionParamValues(CodeFunction member)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var paramsStr = "";

            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
                {
                    _parts.NameSpaces.AddIfNotExists(member.Type.CodeType.Namespace.FullName);
                }

                if (param.Type != null && param.Type.CodeType.Kind == vsCMElement.vsCMElementInterface)
                {
                    //generate interfaces
                    paramsStr += GenerateInterface(param) + ", ";
                }
                else if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    //if the param is a CodeClass we can create an input object for it
                    GenerateFunctionParamForClassInput(param.Type.CodeType.Name, param.Type.AsFullName, param.Type);
                    paramsStr += string.Format("{0}Input, ", param.Name);
                }
                else
                {
                    paramsStr += GetParamValue(param.Type, param.Name, 0) + ", ";
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

            return DTEHelper.GenPrivateClassNameAtTop(codeInterface.Name);
        }

        public string GenerateFunctionParamForClassInput(string name, string fullName, CodeTypeRef codeTypeRef)
        {
            var fullNameToUseAsReturnType = fullName;

            if (ClassGenerator.IsCodeTypeAList(name))
            {
                //var baseType = ((CodeElement)codeTypeRef.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.GetBaseType(fullName));
                var baseType = TryToGuessGenericArgument(codeTypeRef);
                fullNameToUseAsReturnType = string.Format("{0}<{1}>", name, baseType.CodeType.Name);
                name = baseType == null ? "//Couldnt get list type name" : baseType.CodeType.Name + "List";
            }
            else if (name == "Task")
            {
                //var baseType = ((CodeElement)codeTypeRef.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.GetBaseType(fullName));
                var baseType = TryToGuessGenericArgument(codeTypeRef);
                name = baseType == null ? "//Couldnt get type from Task" : baseType.CodeType.Name;
            }

            var exists = _parts.ParamsGenerated.FirstOrDefault(x => x.FullName == fullName);
            if (exists != null) return exists.GetFunctionName; //do not add a 2nd one

            var functionName = string.Format("Get{0}", name);
            //_parts.ParamsGenerated keeps a list of functions that will get the value of the object we generated
            _parts.ParamsGenerated.Add(new ParamsGenerated() { FullName = fullName, GetFunctionName = functionName });

            //  var genner = new ClassGenerator(_parts, _opts);
            var inner = GetParamValue(codeTypeRef, string.Empty, 3);

            var gen = string.Format(@"
        private static {0} {1}() {{
            return {2};
        }}
        ", fullNameToUseAsReturnType, functionName, inner);

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
            //  var baseType = ((CodeElement)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.GetBaseType(member.AsFullName));
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

        public CodeTypeRef TryToGuessGenericArgument(CodeTypeRef member, ProjectItem projItem = null)
        {
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

            var typeNameAsInCode = DTEHelper.RemoveTask(codeTypeRef2.AsFullName);
            // var typeNameAsInCode = codeTypeRef2.AsString.Replace("?", "");
            //typeNameAsInCode = typeNameAsInCode.Split('<', '>').ElementAtOrDefault(1) ?? "";
            typeNameAsInCode = typeNameAsInCode.Split('<', '>').ElementAtOrDefault(1) ?? typeNameAsInCode;

            CodeModel projCodeModel;

            try
            {
                projCodeModel = ((CodeElement)member.Parent).ProjectItem.ContainingProject.CodeModel;
            }
            catch (COMException)
            {
                projCodeModel = GetActiveProject().CodeModel;
            }

            var codeType = projCodeModel.CodeTypeFromFullName(TryToGuessFullName(typeNameAsInCode));

            if (codeType == null && projItem != null)
            {
                codeType = projItem.ContainingProject.CodeModel.CodeTypeFromFullName(TryToGuessFullName(typeNameAsInCode));
            }

            if (codeType != null) return projCodeModel.CreateCodeTypeRef(codeType);
            return member;
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

        public Project GetActiveProject()
        {
            try
            {
                Array activeSolutionProjects = _dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                    return activeSolutionProjects.GetValue(0) as Project;
            }
            catch (Exception ex)
            {
                // Logger.Log("Error getting the active project" + ex);
            }

            return null;
        }

        private CodeTypeRef RemoveNullable(CodeTypeRef member)
        {
            try
            {
                if (member.CodeType != null && member.CodeType.Name == "Nullable")
                {
                    return TryToGuessGenericArgument(member);
                    // return ((CodeProperty)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.RemoveNullableStr(member.AsFullName));
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