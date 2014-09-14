// Guids.cs
// MUST match guids.h
using System;

namespace Company.CleanHiveAndDebugExtension
{
    static class GuidList
    {
        public const string guidCleanHiveAndDebugExtensionPkgString = "f0d5e59a-13e9-4f98-98e0-459b5e883e8b";
        public const string guidCleanHiveAndDebugExtensionCmdSetString = "f88af01a-134f-47b1-a09c-8bab964e6466";

        public static readonly Guid guidCleanHiveAndDebugExtensionCmdSet = new Guid(guidCleanHiveAndDebugExtensionCmdSetString);
    };
}