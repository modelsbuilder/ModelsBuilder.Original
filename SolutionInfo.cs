using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("Umbraco ModelsBuilder")]
[assembly: AssemblyCompany("Umbraco")]
[assembly: AssemblyCopyright("Copyright © Umbraco HQ 2016")]
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

// Vsix
// Also need to

// Assembly
[assembly: AssemblyVersion("3.0.4.0")]
[assembly: AssemblyFileVersion("3.0.4.0")]

// NuGet Package
// Note: could not release "1.8.0" because it was depending on pre-release NuGet packages
//  for Roslyn, so had to release 1.8.0-final... starting with 2.1.3 Roslyn has a released
//  1.0 version, so now we can release "2.1.3" without the "-final" extension.
[assembly: AssemblyInformationalVersion("3.0.4.0")]
// Do not remove this line.
