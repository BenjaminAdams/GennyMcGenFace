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
            HasConstructor = false;
            HasInterfaces = false;
        }

        public string MainClassName { get; set; }
        public string MainNamespace { get; set; }
        public string ParamInputs { get; set; }
        public string InitCode { get; set; }
        public string Tests { get; set; }

        public bool HasConstructor { get; set; }
        public bool HasInterfaces { get; set; }

        public List<string> PrivateClassesAtTop { get; set; }
        public List<string> NameSpaces { get; set; }

        public List<ParamsGenerated> ParamsGenerated { get; set; }

        public string GetParamFunctionName(string functionName)
        {
            var found = ParamsGenerated.FirstOrDefault(x => x.FullName == functionName);
            if (found == null) return "Not Found";
            return found.GetFunctionName;
        }
    }

    public class ParamsGenerated
    {
        public string FullName { get; set; }
        public string GetFunctionName { get; set; }
    }
}