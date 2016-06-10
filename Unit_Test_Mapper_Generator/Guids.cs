// Guids.cs
// MUST match guids.h
using System;

namespace BenjaminAdams.Unit_Test_Mapper_Generator
{
    internal static class GuidList
    {
        public const string guidUnit_Test_Mapper_GeneratorPkgString = "7f28ec20-6ba7-4d68-8a12-77e93761047c";
        public const string guidUnit_Test_Mapper_GeneratorCmdSetString = "16e5a107-d17f-4dba-bf77-e0ee7d367266";
        public const string guidToolWindowPersistanceString = "f9884568-b61b-4376-9aa5-3c1687a95bf4";

        public static readonly Guid guidTWShortcutMenuCmdSet = new Guid("f69209e9-975a-4543-821d-1f4a2c52d737");

        public static readonly Guid guidUnit_Test_Mapper_GeneratorCmdSet = new Guid(guidUnit_Test_Mapper_GeneratorCmdSetString);
    };
}