// Guids.cs
// MUST match guids.h

using System;

namespace ZpqrtBnk.ModelzBuilder.CustomTool
{
    static class GuidList
    {
        public const string PkgString = "d6667f9d-7c28-400f-85b3-cf2fa1d5b09c";
        public const string CmdSetString = "0e7940a5-22e4-4286-961c-19fd4fa175c3";

        public static readonly Guid CmdSet = new Guid(CmdSetString);
    };
}