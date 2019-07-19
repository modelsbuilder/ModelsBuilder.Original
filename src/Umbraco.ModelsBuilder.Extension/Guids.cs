// Guids.cs
// MUST match guids.h

using System;

namespace Umbraco.ModelsBuilder.Extension
{
    static class GuidList
    {
        public const string PkgString = "6a4c1726-440f-4b2d-a2e5-711277da6099";
        public const string CmdSetString = "fb40dc0b-2f75-404c-ba4e-dc1b90c41941";

        public static readonly Guid CmdSet = new Guid(CmdSetString);
    };
}