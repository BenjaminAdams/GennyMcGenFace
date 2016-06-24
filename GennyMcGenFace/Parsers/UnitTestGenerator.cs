using EnvDTE;
using GennyMcGenFace.Helpers;
using GennyMcGenFace.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.Parsers
{
    public class UnitTestGenerator
    {
        private static GenOptions _opts;

        public static string Gen(CodeClass selectedClass, GenOptions opts)
        {
            _opts = opts;

            var parts = new UnitTestParts
            {
                MainClassName = selectedClass.FullName,
                MainNamespace = selectedClass.Namespace.FullName
            };

            ParseFunctions(selectedClass, parts);
            GenInit(selectedClass, parts);

            var outer = PutItAllTogether(parts);
            return outer;
        }

        private static void GenInit(CodeClass selectedClass, UnitTestParts parts)
        {
        }

        private static void ParseFunctions(CodeClass selectedClass, UnitTestParts parts)
        {
            foreach (CodeFunction member in selectedClass.Members.OfType<CodeFunction>())
            {
                if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
                {
                    parts.NameSpaces.AddIfNotExists(member.Type.CodeType.Namespace.FullName);
                }

                if (member.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                {
                    GenerateConstructor(member, parts);
                }
                else
                {
                    GenerateOneTestForAFunction(member, parts);
                }
            }
        }

        private static void GenerateConstructor(CodeFunction member, UnitTestParts parts)
        {
            parts.HasConstructor = true;
            var paramsStr = GenerateFunctionParamValues(member, parts);

            //parts.PrivateClassesAtTop.AddIfNotExists("testTarget");
            parts.InitCode += string.Format("            _testTarget = new {0}({1});\r\n", member.FullName, paramsStr);
        }

        private static void GenerateOneTestForAFunction(CodeFunction member, UnitTestParts parts)
        {
            var paramsStr = GenerateFunctionParamValues(member, parts);

            var str = string.Format(@"
        [TestMethod]
        public void {0}Test()
        {{
            {1}
            var res = _testTarget.{0}({2});
            Assert.IsNotNull(res);
            //Todo: Add more Asserts
        }}
", member.Name, GetInputsBeforeFunctionParams(member, parts), paramsStr);

            parts.Tests += str;
        }

        private static string GetInputsBeforeFunctionParams(CodeFunction member, UnitTestParts parts)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var strInputs = "";
            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                //get an input object for it
                if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    strInputs += string.Format("var input{0} = {1}();\r\n", param.Name, GenerateFunctionParamForClassInput((CodeClass)param.Type.CodeType, parts));
                }
            }

            return strInputs;
        }

        private static string GenerateFunctionParamValues(CodeFunction member, UnitTestParts parts)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var paramsStr = "";

            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
                {
                    parts.NameSpaces.AddIfNotExists(member.Type.CodeType.Namespace.FullName);
                }

                if (param.Type != null && param.Type.CodeType.Kind == vsCMElement.vsCMElementInterface)
                {
                    //generate interface
                    paramsStr += GenerateInterface(param, parts) + ",";
                }
                else if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    //if the param is a CodeClass we can create an input object for it
                    GenerateFunctionParamForClassInput((CodeClass)param.Type.CodeType, parts);
                    paramsStr += string.Format("input{0},", param.Name);
                }
                else
                {
                    //add something to the test init?
                    paramsStr += ClassGenerator.GetParamValue(param.Type, param.Name, 0) + ",";
                }
            }

            paramsStr = paramsStr.TrimEnd(',');
            return paramsStr;
        }

        private static string GenerateInterface(CodeParameter param, UnitTestParts parts)
        {
            var interf = (CodeInterface)param.Type.CodeType;

            parts.PrivateClassesAtTop.AddIfNotExists(interf.Name);
            return GenPrivateClassName(interf.Name);
        }

        private static string GenerateFunctionParamForClassInput(CodeClass param, UnitTestParts parts)
        {
            var functionName = string.Format("Get{0}", param.Name);
            if (parts.ParamsGenerated.Any(x => x.FullName == param.FullName)) return functionName; //do not add a 2nd one

            //parts.ParamsGenerated keeps a list of functions that will get the value of the object we generated
            parts.ParamsGenerated.Add(new ParamsGenerated() { FullName = param.FullName, GetFunctionName = functionName });

            var inner = ClassGenerator.GenerateClassStr(param, _opts, 3).Replace("var obj = ", "");

            var gen = string.Format(@"
        private static {0} {1}() {{
            return new {2}
        }}
        ", param.FullName, functionName, inner);

            parts.ParamInputs += gen;
            return functionName;
        }

        private static string GenPrivateClassName(string className)
        {
            return "_" + className.FirstCharacterToLower();
        }

        private static string GenPrivateClassesAtTop(UnitTestParts parts)
        {
            var frmtStr = "        private {0} {1};\r\n";
            var str = string.Empty;
            foreach (var className in parts.PrivateClassesAtTop)
            {
                var privClassName = GenPrivateClassName(className);
                str += string.Format(frmtStr, className, privClassName);
                parts.InitCode = string.Format("            {0} = Substitute.For<{1}>();\r\n", privClassName, className) + parts.InitCode;
            }

            if (parts.HasConstructor)
            {
                str += string.Format(frmtStr, parts.MainClassName, "_testTarget");
            }

            return str;
        }

        private static string GenNameSpaces(UnitTestParts parts)
        {
            var str = string.Empty;
            foreach (var ns in parts.NameSpaces)
            {
                str += string.Format("using {0};\r\n", ns);
            }

            if (parts.HasInterfaces)
            {
                str += "using NSubstitute;";
            }

            return str;
        }

        private static string PutItAllTogether(UnitTestParts parts)
        {
            return string.Format(@"using Microsoft.VisualStudio.TestTools.UnitTesting;
{0}
namespace {1}
{{
    [TestClass]
    public class {2}Tests
    {{
{3}
        [TestInitialize]
        public void Init()
        {{
{4}
        }}

{5}

        {6}
    }}
}}", GenNameSpaces(parts), parts.MainNamespace, parts.MainClassName.Replace(".", ""), GenPrivateClassesAtTop(parts), parts.InitCode, parts.Tests, parts.ParamInputs);
        }
    }
}