using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Umbraco ModelsBuilder")]
[assembly: AssemblyDescription("Umbraco ModelsBuilder.")]
[assembly: Guid("b59ebab2-bc7c-4a89-876f-7613684510e2")]

[assembly: InternalsVisibleTo("Umbraco.ModelsBuilder.Tests")]
[assembly: InternalsVisibleTo("Umbraco.ModelsBuilder.Api")]

// dynamic assembly that is built during tests - do not remove!
[assembly: InternalsVisibleTo("Umbraco.ModelsBuilder.RunTests")]
