using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("ZpqrtBnk Umbraco ModelsBuilder")]
[assembly: AssemblyCompany("Pilotine - ZpqrtBnk")]
[assembly: AssemblyCopyright("Copyright © Pilotine - ZpqrtBnk 2013-2015")]
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
[assembly: AssemblyVersion("2.1.2.42")]
[assembly: AssemblyFileVersion("2.1.2.42")]

// NuGet Package
// Note: cannot release "1.8.0" because it depends on pre-release NuGet packages
// so I have to use 1.8.0-final...
[assembly: AssemblyInformationalVersion("2.1.2-final")]
// Do not remove this line.