using EnvDTE;
using GennyMcGenFace.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.Parser
{
    public class UnitTestParts
    {
        public UnitTestParts()
        {
            ParamsGenerated = new List<ParamsGenerated>();
        }

        public string MainClassName { get; set; }
        public string ParamInputs { get; set; }
        public string InitCode { get; set; }

        public string PrivateClassesAtTop { get; set; }

        public List<ParamsGenerated> ParamsGenerated { get; set; }
    }

    public class ParamsGenerated
    {
        public string FullName { get; set; }
        public string PrivateFunctionName { get; set; }
    }

    public class UnitTestGenerator
    {
        private static GenOptions _opts;

        public static string Gen(CodeClass selectedClass, GenOptions opts)
        {
            _opts = opts;

            var parts = new UnitTestParts
            {
                MainClassName = selectedClass.FullName
            };

            foreach (CodeFunction member in selectedClass.Members.OfType<CodeFunction>())
            {
                GenerateOneTestForAFunction(member, parts);
            }

            var outer = PutItAllTogether(parts);
            return outer;
        }

        private static void GenerateOneTestForAFunction(CodeFunction member, UnitTestParts parts)
        {
            foreach (CodeParameter param in member.Parameters.OfType<CodeParameter>())
            {
                //if the param is a CodeClass we can create a input object for it
                if (param.Type != null && param.Type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                {
                    GenerateFunctionParam((CodeClass)param.Type.CodeType, parts);
                }
            }
        }

        private static void GenerateFunctionParam(CodeClass param, UnitTestParts parts)
        {
            if (parts.ParamsGenerated.Any(x => x.FullName == param.FullName)) return; //do not add a 2nd one

            var functionName = string.Format("Get{0}", param.Name);
            var paramStr = string.Format("\r\nprivate static {0} {1}() {{\r\n", param.FullName, functionName);
            paramStr += "return new ";
            paramStr += ClassGenerator.GenerateClassStr(param, _opts).Replace("var obj = ", "");
            paramStr += "};\r\n}\r\n";
            parts.ParamInputs += paramStr;
            parts.ParamsGenerated.Add(new ParamsGenerated() { FullName = param.FullName, PrivateFunctionName = functionName });
        }

        private static UnitTestParts GetUnitTestParts()
        {
            return new UnitTestParts()
            {
            };
        }

        private static string PutItAllTogether(UnitTestParts parts)
        {
            return string.Format(@"
                    using Microsoft.VisualStudio.TestTools.UnitTesting;\r\n
                    using NSubstitute;\r\n
                    \r\n
                    namespace Your.NameSpace\r\n
                    {{\r\n
                        [TestClass]\r\n
                        public class {0}Tests\r\n
                        {{\r\n
                            {1}\r\n
                            private IB2BResponseProcess _b2BResponseProcess;\r\n
                            private B2BController _b2BController;\r\n
                            \r\n
                            [TestInitialize]\r\n
                            public void Init()\r\n
                            {{\r\n
                                //var log = Substitute.For<ICustomLog>();
                                //_b2BResponseProcess = Substitute.For<IB2BResponseProcess>();
                                //b2BController = new B2BController(_b2BResponseProcess, log);
                                {2}
			                    \r\n
                            }}
		                    \r\n
                            {3}\r\n
	                    }}\r\n
	                    \r\n
                    }}", parts.MainClassName, parts.PrivateClassesAtTop, parts.InitCode, parts.ParamInputs);
        }
    }
}