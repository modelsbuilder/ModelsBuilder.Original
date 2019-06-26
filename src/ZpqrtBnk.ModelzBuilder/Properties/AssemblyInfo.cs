using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ZpqrtBnk ModelzBuilder")]
[assembly: AssemblyDescription("ZpqrtBnk ModelzBuilder.")]
[assembly: Guid("b59ebab2-bc7c-4a89-876f-7613684510e2")]

[assembly: InternalsVisibleTo("ZpqrtBnk.ModelzBuilder.Tests")]
[assembly: InternalsVisibleTo("ZpqrtBnk.ModelzBuilder.Api")]

// dynamic assembly that is built during tests - do not remove!
[assembly: InternalsVisibleTo("ZpqrtBnk.ModelzBuilder.RunTests")]

// code analysis
// IDE1006 is broken, wants _value syntax for consts, etc - and it's even confusing ppl at MS, kill it
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "~_~")]
