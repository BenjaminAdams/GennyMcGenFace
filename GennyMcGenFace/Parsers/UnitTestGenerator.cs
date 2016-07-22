using EnvDTE;
using EnvDTE80;
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
        private ClassGenerator _genner;
        private DTE2 _dte;

        public UnitTestGenerator(CodeClass selectedClass, DTE2 dte)
        {
            _dte = dte;

            _parts = new UnitTestParts
            {
                MainClassName = selectedClass.FullName,
                MainNamespace = selectedClass.Namespace.FullName,
                SelectedClass = selectedClass,
                NameSpaces = new List<string>() { selectedClass.Namespace.FullName },
                IsStaticClass = selectedClass.IsAbstract
            };
        }

        public string Gen(CodeClass selectedClass, GenOptions opts)
        {
            _opts = opts;
            _genner = new ClassGenerator(_parts, _opts, _dte);

            ParseFunctions(selectedClass);

            return PutItAllTogether();
        }

        private void ParseFunctions(CodeClass selectedClass)
        {
            var isStatic = true;
            var constructorsGenerated = 0;
            foreach (CodeFunction member in selectedClass.Members.OfType<CodeFunction>())
            {
                if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
                {
                    _parts.NameSpaces.AddIfNotExists(member.Type.CodeType.Namespace.FullName);
                }

                if (member.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                {
                    GenerateConstructor(member);
                    constructorsGenerated++;
                }
                else
                {
                    GenerateOneTestForAFunction(member);
                    if (member.IsShared == false)
                    {
                        //we are assuming if there is one non-static function in the class then the entire class is non-static
                        //The Docs claim CodeClass.IsShared property is available, but this prop is hidden for CodeClass
                        isStatic = false;
                    }
                }
            }

            _parts.IsStaticClass = isStatic;

            try
            {
                if (selectedClass.IsAbstract == false && isStatic == false && constructorsGenerated == 0)
                {
                    GenerateEmptyConstructor();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void GenerateConstructor(CodeFunction member)
        {
            _parts.HasConstructor = true;
            var paramsStr = _genner.GenerateFunctionParamValues(member);

            _parts.InitCode += string.Format("{0}_testTarget = new {1}({2});\r\n", Spacing.Get(3), member.Name, paramsStr);
        }

        private void GenerateEmptyConstructor()
        {
            _parts.HasConstructor = true;

            _parts.InitCode += string.Format("{0}_testTarget = new {1}();\r\n", Spacing.Get(3), _parts.SelectedClass.Name);
        }

        private string GetInputsBeforeFunctionParams(CodeFunction member)
        {
            if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
            var strInputs = "";
            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    //get an input object for the class input
                    strInputs += string.Format("{0}var {1}Input = {2}();\r\n", Spacing.Get(3), param.Name, _genner.GenerateFunctionParamForClassInput(param.Name, param.Type.AsFullName, param.Type));
                }
            }

            return strInputs;
        }

        //private string GenerateFunctionParamValues(CodeFunction member)
        //{
        //    if (member.Parameters == null || member.Parameters.OfType<CodeParameter>().Any() == false) return string.Empty;
        //    var paramsStr = "";

        //    foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
        //    {
        //        if (member.Type != null && member.Type.CodeType != null && member.Type.CodeType.Namespace != null)
        //        {
        //            _parts.NameSpaces.AddIfNotExists(member.Type.CodeType.Namespace.FullName);
        //        }

        //        if (param.Type != null && param.Type.CodeType.Kind == vsCMElement.vsCMElementInterface)
        //        {
        //            //generate interfaces
        //            paramsStr += GenerateInterface(param) + ", ";
        //        }
        //        else if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
        //        {
        //            //if the param is a CodeClass we can create an input object for it
        //            GenerateFunctionParamForClassInput(param.Type.CodeType.Name, param.Type.AsFullName, param.Type);
        //            paramsStr += string.Format("{0}Input, ", param.Name);
        //        }
        //        else
        //        {
        //            //var genner = new ClassGenerator(_parts, _opts);
        //            paramsStr += _genner.GetParamValue(param.Type, param.Name, 0) + ", ";
        //        }
        //    }

        //    paramsStr = paramsStr.Trim().TrimEnd(',');
        //    return paramsStr;
        //}

        private string GenPrivateClassesAtTop()
        {
            var frmtStr = Spacing.Get(2) + "private {0} {1};\r\n";
            var str = string.Empty;
            foreach (var className in _parts.PrivateClassesAtTop)
            {
                var privClassName = DTEHelper.GenPrivateClassNameAtTop(className);
                str += string.Format(frmtStr, className, privClassName);
                _parts.InitCode = string.Format("{0}{1} = Substitute.For<{2}>();\r\n", Spacing.Get(3), privClassName, className) + _parts.InitCode;
            }

            if (_parts.HasConstructor || _parts.IsStaticClass == false)
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
                    try
                    {
                        var returnType = string.Empty;

                        var isAsync = member.Type.CodeType.FullName.Contains("System.Threading.Tasks.Task");
                        // var baseType = ((CodeElement)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.GetBaseType(member.Type.CodeType.FullName));
                        ProjectItem projItem = null;

                        try
                        {
                            if (member.ProjectItem != null)
                            {
                                projItem = member.ProjectItem;
                            }
                        }
                        catch { }
                     

                        var baseType = _genner.TryToGuessGenericArgument(member.Type, projItem);

                        if (member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefVoid || (baseType != null && baseType.AsFullName == "System.Threading.Tasks.Task"))
                        {
                            continue; //no need to mock return type for Void functions
                        }

                        if (baseType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                        {
                            _genner.GenerateFunctionParamForClassInput(baseType.CodeType.Name, baseType.CodeType.FullName, baseType);
                            //_genner.GenerateFunctionParamForClassInput(member.Type.CodeType.Name, member.Type.CodeType.FullName, member.Type); //make sure we have created this type so we can return it
                            returnType = _parts.GetParamFunctionName(member.Type.AsFullName) + "()";
                        }
                        else
                        {
                            // var genner = new ClassGenerator(_parts, _opts);
                            returnType = _genner.GetParamValue(baseType, "", 0);
                        }

                        if (isAsync)
                        {
                            returnType = string.Format("Task.FromResult({0})", returnType);
                        }

                        str += string.Format("{0}{1}.{2}({3}).Returns({4});\r\n", Spacing.Get(3), DTEHelper.GenPrivateClassNameAtTop(face.Name), member.Name, GetInterfaceArgs(member), returnType);
                    }
                    catch (Exception ex)
                    {
                        str += string.Format("{0}//Could not generate {1};\r\n", Spacing.Get(3), member.Name);
                    }
                }
            }

            str = str.ReplaceLastOccurrence("\r\n", "");
            return str;
        }

        /// <summary>
        /// Returns a string contains the args needed for nsub to mock the return obj
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        private string GetInterfaceArgs(CodeFunction face)
        {
            //goal a string= "Arg.Any<Guid>(), Arg.Any<string>()"
            var paramsStr = string.Empty;

            foreach (CodeParameter param in face.Parameters.OfType<CodeParameter>())
            {
                var name = param.Type.AsString.Replace("System.Collections.Generic.", ""); //we can prolly trust generic list as their type in shortname

                paramsStr += string.Format("Arg.Any<{0}>(), ", name);
            }

            paramsStr = paramsStr.TrimEnd().TrimEnd(',');
            return paramsStr;
        }

        private string GetFunctionName(string name)
        {
            if (_parts.FunctionNamesCreated.Contains(name))
            {
                var rnd = new Random();
                name = name + rnd.Next(1, 99999);
            }

            _parts.FunctionNamesCreated.AddIfNotExists(name);

            return name;
        }

        private string GenerateAssertsForFunction(CodeFunction member)
        {
            return "";
        }

        private void GenerateOneTestForAFunction(CodeFunction member)
        {
            try
            {
                var isAsync = member.Type.CodeType.FullName.Contains("System.Threading.Tasks.Task");
                //  var baseType = ((CodeElement)member.Parent).ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(DTEHelper.GetBaseType(member.Type.CodeType.FullName));
                var baseType = _genner.TryToGuessGenericArgument(member.Type);

                var paramsStr = _genner.GenerateFunctionParamValues(member);

                var returnsValCode = "var res = ";
                var testReturnType = "void";
                var functionTargetName = "_testTarget";

                var afterFunction = "Assert.IsNotNull(res);\r\n";
                if (member.Type != null && member.Type.TypeKind == vsCMTypeRef.vsCMTypeRefVoid ||
                    (baseType != null && baseType.AsFullName == "System.Threading.Tasks.Task"))
                {
                    returnsValCode = "";
                    afterFunction = "";
                }
                else
                {
                    //todo generate asserts based on the return type
                    afterFunction += GenerateAssertsForFunction(member);
                }

                if (isAsync)
                {
                    returnsValCode += "await ";
                    testReturnType = "async Task";
                }

                if (member.IsShared) //IsShared means its a static function
                {
                    functionTargetName = _parts.SelectedClass.Name;
                }

                var functionName = GetFunctionName(member.Name);

                var str = string.Format(@"
        [TestMethod]
        public {0} {1}Test()
        {{
{2}
            {3}{6}.{1}({4});
            {5}
        }}
", testReturnType, functionName, GetInputsBeforeFunctionParams(member), returnsValCode, paramsStr, afterFunction, functionTargetName);

                _parts.Tests += str;
            }
            catch (Exception ex)
            {
                _parts.Tests += string.Format(@"
Unable to generate unit test for {0}
", member.FullName);
            }
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
{6}{7}
    }}
}}", GenNameSpaces(), _parts.MainNamespace, _parts.MainClassName.Replace(".", "_"), GenPrivateClassesAtTop(), _parts.InitCode, GenInterfaceMocking(), _parts.Tests, _parts.ParamInputs);
        }
    }
}