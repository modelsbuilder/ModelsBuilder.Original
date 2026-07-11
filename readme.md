#### Umbraco Models Builder

Copyright (C) The Umbraco Community 2013-2026
Distributed under the MIT license  

A tool that can generate a complete set of strongly-typed published content models for Umbraco.
Models are available in controllers, views, anywhere.
Runs either from the Umbraco UI, from the command line, or from Visual Studio.

**WARNING** Last time that project was seriously updated is 2020.
It may be updated from time to time to satisfy the needs of the very few users who still depend on it.
But everything you read should be taken with a grain of salt. Parts of the project (VSIX...) are not maintained at all.
You have been warned.

#### Documentation

Infos, can be found in the [wiki](https://github.com/OurModelsBuilder/Our.ModelsBuilder/wiki).

**WARNING** That wiki is 10+ years old and probably mostly obsolete.

#### Building

Go to `src/Our.ModelsBuilder` and run `dotnet build -c Release`, there will be a NuGet package in `bin/Release`.

All other projects or packages are not supported anymore, you are on your own.

#### Projects

*Umbraco.ModelsBuilder - the main project, installed on the website
*Umbraco.ModelsBuilder.Api - the api server **OBSOLETE**
*Umbraco.ModelsBuilder.Console - a console tool **OBSOLETE**
*Umbraco.ModelsBuilder.CustomTool - the Visual Studio extension **OBSOLETE**
*Umbraco.ModelsBuilder.Tests - the tests suite

Both .Console and .CustomTool require that the .Api is installed on the website (not installed by default).
