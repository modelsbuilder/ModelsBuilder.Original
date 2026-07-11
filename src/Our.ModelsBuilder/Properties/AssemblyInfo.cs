using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Our.ModelsBuilder.Tests")]

// dynamic assembly that is built during tests - do not remove!
[assembly: InternalsVisibleTo("Our.ModelsBuilder.RunTests")]

// code analysis
// IDE1006 is broken, wants _value syntax for consts, etc - and it's even confusing ppl at MS, kill it
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "~_~")]
