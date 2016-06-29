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

            return PutItAllTogether();
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
                    GenerateConstructor(member);
                }
                else
                {
                    GenerateOneTestForAFunction(member);
                }
            }
        }

        private void GenerateConstructor(CodeFunction member)
        {
            _parts.HasConstructor = true;
            var paramsStr = GenerateFunctionParamValues(member);

            _parts.InitCode += string.Format("{0}_testTarget = new {1}({2});\r\n", Spacing.Get(2), member.Name, paramsStr);
        }

        private string GetInputsBeforeFunctionParams(CodeFunction member)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var strInputs = "";
            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                //get an input object for it
                if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    strInputs += string.Format("var {0}Input = {1}();\r\n", param.Name, GenerateFunctionParamForClassInput(param.Name, param.Type.AsFullName, param.Type));
                }
            }

            return strInputs;
        }

        private string GenerateFunctionParamValues(CodeFunction member)
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
                    var genner = new ClassGenerator(_parts, _opts);
                    paramsStr += genner.GetParamValue(param.Type, param.Name, 0) + ", ";
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

            return GenPrivateClassNameAtTop(codeInterface.Name);
        }

        private string GenerateFunctionParamForClassInput(string name, string fullName, CodeTypeRef codeTypeRef)
        {
            if (ClassGenerator.IsCodeTypeAList(name))
            {
                var baseType = ((CodeElement)codeTypeRef.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.GetBaseTypeFromList(fullName));
                name = baseType == null ? "//Couldnt get list type name" : baseType.CodeType.Name + "List";
            }

            var exists = _parts.ParamsGenerated.FirstOrDefault(x => x.FullName == fullName);
            if (exists != null) return exists.GetFunctionName; //do not add a 2nd one

            var functionName = string.Format("Get{0}", name);
            //_parts.ParamsGenerated keeps a list of functions that will get the value of the object we generated
            _parts.ParamsGenerated.Add(new ParamsGenerated() { FullName = fullName, GetFunctionName = functionName });

            var genner = new ClassGenerator(_parts, _opts);
            var inner = genner.GetParamValue(codeTypeRef, string.Empty, 2);

            var gen = string.Format(@"
        private static {0} {1}() {{
            return {2};
        }}
        ", fullName, functionName, inner);

            _parts.ParamInputs += gen;
            return functionName;
        }

        private static string GenPrivateClassNameAtTop(string className)
        {
            return "_" + className.FirstCharacterToLower();
        }

        private string GenPrivateClassesAtTop()
        {
            var frmtStr = Spacing.Get(2) + "private {0} {1};\r\n";
            var str = string.Empty;
            foreach (var className in _parts.PrivateClassesAtTop)
            {
                var privClassName = GenPrivateClassNameAtTop(className);
                str += string.Format(frmtStr, className, privClassName);
                _parts.InitCode = string.Format(Spacing.Get(2) + "{0} = Substitute.For<{1}>();\r\n", privClassName, className) + _parts.InitCode;
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

            if (_parts.Interfaces.Any())
            {
                str += "using NSubstitute;\r\n";
            }

            str += "using System.Collections.Generic;\r\n";

            return str;
        }

        private string GenInterfaceMocking()
        {
            //goal
            //async   _someClass.SomeFunction(Arg.Any<Guid>()).Returns(Task.FromResult(SomeClass));
            //reg     _someClass.SomeFunction(Arg.Any<Guid>()).Returns(SomeClass);
            var str = string.Empty;
            foreach (CodeInterface face in _parts.Interfaces)   //foreach interface found in our test class
            {
                if (face.Kind != vsCMElement.vsCMElementInterface) continue;

                foreach (CodeFunction member in face.Members.OfType<CodeFunction>()) //foreach function in interface
                {
                    var returnType = string.Empty;
                    if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefVoid)
                    {
                        continue; //no need to mock return type for Void functions
                    }

                    if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                    {
                        GenerateFunctionParamForClassInput(member.Type.CodeType.Name, member.Type.CodeType.FullName, member.Type); //make sure we have created this type so we can return it
                        returnType = _parts.GetParamFunctionName(member.Type.AsFullName) + "()";
                    }
                    else
                    {
                        var genner = new ClassGenerator(_parts, _opts);
                        returnType = genner.GetParamValue(member.Type, "", 0);
                    }

                    str += string.Format("{0}{1}.{2}({3})\r\n{4}.Returns({5});\r\n",
                        Spacing.Get(2), GenPrivateClassNameAtTop(face.Name), member.Name, GetInterfaceArgs(face), Spacing.Get(4), returnType);
                }
            }

            return str;
        }

        /// <summary>
        /// Returns a string contains the args needed for nsub to mock the return obj
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        private string GetInterfaceArgs(CodeInterface face)
        {
            //goal Arg.Any<Guid>(), Arg.Any<string>()
            var paramsStr = string.Empty;

            foreach (CodeFunction member in face.Members.OfType<CodeFunction>())
            {
                foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
                {
                    paramsStr += string.Format("Arg.Any<{0}>(), ", param.Type.AsFullName.RemoveSystemFromStr());
                }
            }
            paramsStr = paramsStr.TrimEnd().TrimEnd(',');
            return paramsStr;
        }

        private void GenerateOneTestForAFunction(CodeFunction member)
        {
            var paramsStr = GenerateFunctionParamValues(member);

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
        }}", member.Name, GetInputsBeforeFunctionParams(member), returnsValCode, paramsStr, afterFunction);

            _parts.Tests += str;
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
{5}
        }}
{6}
{7}
    }}
}}", GenNameSpaces(), _parts.MainNamespace, _parts.MainClassName.Replace(".", "_"), GenPrivateClassesAtTop(), _parts.InitCode, GenInterfaceMocking(), _parts.Tests, _parts.ParamInputs);
        }
    }
}