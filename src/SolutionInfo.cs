using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("Umbraco ModelsBuilder")]
[assembly: AssemblyCompany("Umbraco")]
[assembly: AssemblyCopyright("Copyright © Umbraco HQ 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

// versionning
// nuget/semver: major.minor.patch [-xxx]
// assembly: major.minor.patch.build
// nuget sorts the -xxx alphabetically

// nuget            assembly
// 1.8.0-alpha001   1.8.0.0
// 1.8.0-alpha002   1.8.0.1
// 1.8.0-alpha      1.8.0.2
// 1.8.0-beta001    1.8.0.3
// 1.8.0-beta002    1.8.0.4
// 1.8.0-beta       1.8.0.5
// 1.8.0            1.8.0.6

// versions
// read https://stackoverflow.com/questions/64602/what-are-differences-between-assemblyversion-assemblyfileversion-and-assemblyin

// this is the ONLY ONE the CLR cares about for compatibility
// should change ONLY when "hard" breaking compatibility (manual change)
[assembly: AssemblyVersion("8.0.0")]

// ... good to have a build number ...
[assembly: AssemblyFileVersion("8.0.0.7")]

// NuGet Package
// Note: could not release "1.8.0" because it was depending on pre-release NuGet packages
//  for Roslyn, so had to release 1.8.0-final... starting with 2.1.3 Roslyn has a released
//  1.0 version, so now we can release "2.1.3" without the "-final" extension.
[assembly: AssemblyInformationalVersion("8.0.0-alpha.10")]
// Do not remove this line.
