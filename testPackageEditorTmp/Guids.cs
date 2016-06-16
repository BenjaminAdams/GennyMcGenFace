// Guids.cs
// MUST match guids.h
using System;

namespace Microsoftdfsdf.testPackageEditorTmp
{
    static class GuidList
    {
        public const string guidtestPackageEditorTmpPkgString = "331d3899-668b-4fe5-bf14-eb652d05a976";
        public const string guidtestPackageEditorTmpCmdSetString = "27c32004-c50f-4ec8-8023-a8e297c74c10";
        public const string guidtestPackageEditorTmpEditorFactoryString = "558b9cc1-fb67-411c-80f0-cfd2562546b1";

        public static readonly Guid guidtestPackageEditorTmpCmdSet = new Guid(guidtestPackageEditorTmpCmdSetString);
        public static readonly Guid guidtestPackageEditorTmpEditorFactory = new Guid(guidtestPackageEditorTmpEditorFactoryString);
    };
}