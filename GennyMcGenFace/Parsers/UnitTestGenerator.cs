using EnvDTE;
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
                Namespace = selectedClass.Namespace.FullName
            };

            ParseFunctions(selectedClass, parts);

            var outer = PutItAllTogether(parts);
            return outer;
        }

        private static void ParseFunctions(CodeClass selectedClass, UnitTestParts parts)
        {
            foreach (CodeFunction member in selectedClass.Members.OfType<CodeFunction>())
            {
                if (member.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                {
                    GenerateConstructors(member, parts);
                }
                else
                {
                    GenerateOneTestForAFunction(member, parts);
                }
            }
        }

        private static void GenerateConstructors(CodeFunction member, UnitTestParts parts)
        {
            GenerateFunctionParamValues(member, parts);
        }

        private static void GenerateOneTestForAFunction(CodeFunction member, UnitTestParts parts)
        {
            var paramsStr = GenerateFunctionParamValues(member, parts);

            var str = string.Format(@"
        [TestMethod]
        public void {0}Test()
        {{
            {1}
            var res = _mainFunction.{0}({2});

            Assert.IsNotNull(res);
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
                //if the param is a CodeClass we can create an input object for it
                if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
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

        private static string PutItAllTogether(UnitTestParts parts)
        {
            return string.Format(@"using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace {0}
{{
    [TestClass]
    public class {1}Tests
    {{
        {2}
        private IB2BResponseProcess _b2BResponseProcess;
        private B2BController _b2BController;

        [TestInitialize]
        public void Init()
        {{
            //var log = Substitute.For<ICustomLog>();
            //_b2BResponseProcess = Substitute.For<IB2BResponseProcess>();
            //b2BController = new B2BController(_b2BResponseProcess, log);
            {3}
        }}

{4}

        {5}
    }}
}}", parts.Namespace, parts.MainClassName, parts.PrivateClassesAtTop, parts.InitCode, parts.Tests, parts.ParamInputs);
        }
    }
}