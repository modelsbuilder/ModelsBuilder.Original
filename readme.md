#### Umbraco Models Builder

Copyright (C) Umbraco ZpqrtBnk 2013-2016
Distributed under the MIT license  

A tool that can generate a complete set of strongly-typed published content models for Umbraco.
Models are available in controllers, views, anywhere.
Runs either from the Umbraco UI, from the command line, or from Visual Studio.

Requires Umbraco 7.1.4 or later.

#### Documentation

More infos, including a (hopefully) **complete documentation**, can be found in the [wiki](https://github.com/zpqrtbnk/Zbu.ModelsBuilder/wiki/Zbu.ModelsBuilder).

**WARNING** the documentation has not been updated for version 3 (Umbraco.ModelsBuilder) yet.

#### Building

Simply building the solution (in Visual Studio) either in Debug or Release does NOT build
any NuGet package. Building in Debug mode does NOT build the VSIX package, but building in
Release mode DOES build the VSIX package.

**Important** before releasing a new version, ensure that Umbraco.ModelsBuilder.Api.ApiVersion
contains the proper constants for API client/server version check.

In order to build the NuGet package and the VSIX package,
use the build.ps1 Powershell script:

To build version 1.2.3.45 (aka release 1.2.3)
build.ps1 1.2.3 45

To build version 1.2.3.45 beta001 (aka pre-release 1.2.3-beta001)
build.ps1 1.2.3 45 beta001

The "45" number should be incremented each time we release, so that
version 1.2.3-beta001 has assemblies with version 1.2.3.45
version 1.2.3-beta002 has assemblies with version 1.2.3.46
version 1.2.3 (final) has assemblies with version 1.2.3.47

This will create directory build/Release/v1.2.3-whatever containing:
- Umbraco.ModelsBuilder.1.2.3-whatever.nuget = main NuGet package
- Umbraco.ModelsBuilder.Api.1.2.3-whatever.nuget = api server NuGet package
- Umbraco.ModelsBuilder.CustomTool-1.2.3-whatever.vsix = Visual Studio Extension

Note: we are not building an Umbraco package anymore.

#### Projects

*Umbraco.ModelsBuilder - the main project, installed on the website
*Umbraco.ModelsBuilder.Api - the api server
*Umbraco.ModelsBuilder.Console - a console tool
*Umbraco.ModelsBuilder.CustomTool - the Visual Studio extension
*Umbraco.ModelsBuilder.Tests - the tests suite

Both .Console and .CustomTool require that the .Api is installed on the website (not installed by default).