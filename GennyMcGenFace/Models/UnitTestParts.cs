using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.Models
{
    public class UnitTestParts
    {
        public UnitTestParts()
        {
            ParamsGenerated = new List<ParamsGenerated>();
            Tests = "";
            ParamInputs = "";
            InitCode = "";
            PrivateClassesAtTop = new List<string>();
            NameSpaces = new List<string>();
            Interfaces = new List<CodeInterface>();
            FunctionNamesCreated = new List<string>();
            HasConstructor = false;
        }

        public string MainClassName { get; set; }
        public string MainNamespace { get; set; }
        public string ParamInputs { get; set; }
        public string InitCode { get; set; }
        public string Tests { get; set; }

        public bool HasConstructor { get; set; }

        public bool IsStaticClass { get; set; }

        public List<string> PrivateClassesAtTop { get; set; }
        public List<string> FunctionNamesCreated { get; set; }
        public List<string> NameSpaces { get; set; }
        public List<CodeInterface> Interfaces { get; set; }

        public List<ParamsGenerated> ParamsGenerated { get; set; }
        public CodeClass SelectedClass { get; set; }

        public string GetParamFunctionName(string functionName)
        {
            var found = ParamsGenerated.FirstOrDefault(x => x.FullName == functionName);
            if (found == null) return null;
            return found.GetFunctionName;
        }
    }

    public class ParamsGenerated
    {
        public string FullName { get; set; }
        public string GetFunctionName { get; set; }
    }
}