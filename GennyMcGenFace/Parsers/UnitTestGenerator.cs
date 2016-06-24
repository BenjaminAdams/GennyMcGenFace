using EnvDTE;
using GennyMcGenFace.Helpers;
using GennyMcGenFace.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.Parsers
{
    public class UnitTestGenerator
    {
        private static GenOptions _opts;
        private UnitTestParts _parts;

        public UnitTestGenerator(CodeClass selectedClass)
        {
            _parts = new UnitTestParts
            {
                MainClassName = selectedClass.FullName,
                MainNamespace = selectedClass.Namespace.FullName,
                SelectedClass = selectedClass,
                NameSpaces = new List<string>() { selectedClass.Namespace.FullName }
            };
        }

        public string Gen(CodeClass selectedClass, GenOptions opts)
        {
            _opts = opts;

            ParseFunctions(selectedClass);
            GenInit(selectedClass);

            var outer = PutItAllTogether();
            return outer;
        }

        private static void GenInit(CodeClass selectedClass)
        {
        }

        private void ParseFunctions(CodeClass selectedClass)
        {
            foreach (CodeFunction member in selectedClass.Members.OfType<CodeFunction>())
            {
                if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
                {
                    _parts.NameSpaces.AddIfNotExists(member.Type.CodeType.Namespace.FullName);
                }

                if (member.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                {
                    GenerateConstructor(member, _parts);
                }
                else
                {
                    GenerateOneTestForAFunction(member, _parts);
                }
            }
        }

        private void GenerateConstructor(CodeFunction member, UnitTestParts parts)
        {
            _parts.HasConstructor = true;
            var paramsStr = GenerateFunctionParamValues(member, parts);

            _parts.InitCode += string.Format("            _testTarget = new {0}({1});\r\n", member.Name, paramsStr);
        }

        private void GenerateOneTestForAFunction(CodeFunction member, UnitTestParts parts)
        {
            var paramsStr = GenerateFunctionParamValues(member, parts);

            var returnsValCode = "var res = ";
            var afterFunction = "Assert.IsNotNull(res);\r\n";
            if (member.Type != null && member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefVoid)
            {
                returnsValCode = "";
                afterFunction = "";
            }

            var str = string.Format(@"
        [TestMethod]
        public void {0}Test()
        {{
            {1}
            {2}_testTarget.{0}({3});
            {4}
        }}", member.Name, GetInputsBeforeFunctionParams(member, parts), returnsValCode, paramsStr, afterFunction);

            _parts.Tests += str;
        }

        private string GetInputsBeforeFunctionParams(CodeFunction member, UnitTestParts parts)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var strInputs = "";
            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                //get an input object for it
                if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    strInputs += string.Format("var input{0} = {1}();\r\n", param.Name.FirstCharacterToUpper(), GenerateFunctionParamForClassInput((CodeClass)param.Type.CodeType, parts));
                }
            }

            return strInputs;
        }

        private string GenerateFunctionParamValues(CodeFunction member, UnitTestParts parts)
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
                    var genner = new ClassGenerator(_parts);
                    paramsStr += genner.GetParamValue(param.Type, param.Name, 0) + ",";
                }
            }

            paramsStr = paramsStr.TrimEnd(',');
            return paramsStr;
        }

        private string GenerateInterface(CodeParameter param, UnitTestParts parts)
        {
            var interf = (CodeInterface)param.Type.CodeType;

            _parts.PrivateClassesAtTop.AddIfNotExists(interf.Name);
            _parts.HasInterfaces = true;
            return GenPrivateClassName(interf.Name);
        }

        private string GenerateFunctionParamForClassInput(CodeClass param, UnitTestParts parts)
        {
            var functionName = string.Format("Get{0}", param.Name);
            if (_parts.ParamsGenerated.Any(x => x.FullName == param.FullName)) return functionName; //do not add a 2nd one

            //_parts.ParamsGenerated keeps a list of functions that will get the value of the object we generated
            _parts.ParamsGenerated.Add(new ParamsGenerated() { FullName = param.FullName, GetFunctionName = functionName });

            var genner = new ClassGenerator(_parts);
            var inner = genner.GenerateClassStr(param, _opts, 2).Replace("var obj = ", "");

            var gen = string.Format(@"
        private static {0} {1}() {{
            return {2}
        }}
        ", param.FullName, functionName, inner);

            _parts.ParamInputs += gen;
            return functionName;
        }

        private static string GenPrivateClassName(string className)
        {
            return "_" + className.FirstCharacterToLower();
        }

        private string GenPrivateClassesAtTop()
        {
            var frmtStr = "        private {0} {1};\r\n";
            var str = string.Empty;
            foreach (var className in _parts.PrivateClassesAtTop)
            {
                var privClassName = GenPrivateClassName(className);
                str += string.Format(frmtStr, className, privClassName);
                _parts.InitCode = string.Format("            {0} = Substitute.For<{1}>();\r\n", privClassName, className) + _parts.InitCode;
            }

            if (_parts.HasConstructor)
            {
                str += string.Format(frmtStr, _parts.SelectedClass.Name, "_testTarget");
            }

            return str;
        }

        private string GenNameSpaces()
        {
            var str = string.Empty;
            foreach (var ns in _parts.NameSpaces)
            {
                str += string.Format("using {0};\r\n", ns);
            }

            if (_parts.HasInterfaces)
            {
                str += "using NSubstitute;\r\n";
            }

            return str;
        }

        private string PutItAllTogether()
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
}}", GenNameSpaces(), _parts.MainNamespace, _parts.MainClassName.Replace(".", ""), GenPrivateClassesAtTop(), _parts.InitCode, _parts.Tests, _parts.ParamInputs);
        }
    }
}