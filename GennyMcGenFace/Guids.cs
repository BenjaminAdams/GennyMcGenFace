// Guids.cs
// MUST match guids.h
using System;

namespace GennyMcGenFace.GennyMcGenFace
{
    static class GuidList
    {
        public const string guidGennyMcGenFacePkgString = "668eccdc-c4ea-43d2-95fb-8c7a3a1d5bcc";
        public const string guidGennyMcGenFaceCmdSetString = "d3dcaf60-0409-47ec-adb4-6f120ed8dff4";

        public static readonly Guid guidGennyMcGenFaceCmdSet = new Guid(guidGennyMcGenFaceCmdSetString);
    };
}