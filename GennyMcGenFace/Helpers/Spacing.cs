using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.Helpers
{
    public static class Spacing
    {
        public static string Get(int depth)
        {
            var spaces = "";
            for (var i = 0; i < depth; i++)
            {
                spaces += "    ";
            }

            return spaces;
        }
    }
}