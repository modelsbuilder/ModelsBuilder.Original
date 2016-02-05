param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("\d+?\.\d+?\.\d")]
	[string]
	$ReleaseVersionNumber,
	[Parameter(Mandatory=$true)]
	[ValidatePattern("\d")]
	[string]
	$BuildNumber,
	[Parameter(Mandatory=$true)]
	[string]
	[AllowEmptyString()]
	$PreReleaseName
)

if (-not [System.String]::IsNullOrWhitespace($PreReleaseName) -and -not $PreReleaseName.StartsWith("-"))
{
    $PreReleaseName = "-" + $PreReleaseName
}

$PSScriptFilePath = Get-Item $MyInvocation.MyCommand.Path
$RepoRoot = $PSScriptFilePath.Directory.Parent.FullName
$BuildFolder = Join-Path -Path $RepoRoot -ChildPath "build";
$ReleaseFolder = Join-Path -Path $BuildFolder -ChildPath "Release\v$ReleaseVersionNumber$PreReleaseName";
$SolutionRoot = $RepoRoot;
$ProgFiles86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)");
$MSBuild = "$ProgFiles86\MSBuild\14.0\Bin\MSBuild.exe"

# Edit VSIX
$vsixFile = "$SolutionRoot\Umbraco.ModelsBuilder.CustomTool\source.extension.vsixmanifest"
[xml] $vsixXml = Get-Content $vsixFile
$xmlNameTable = New-Object System.Xml.NameTable
$xmlNameSpace = New-Object System.Xml.XmlNamespaceManager($xmlNameTable)
$xmlNameSpace.AddNamespace("vsx", "http://schemas.microsoft.com/developer/vsx-schema/2011")
$xmlNameSpace.AddNamespace("d", "http://schemas.microsoft.com/developer/vsx-schema-design/2011")
$versionNode = $vsixXml.SelectSingleNode("/vsx:PackageManifest/vsx:Metadata/vsx:Identity/@Version", $xmlNameSpace)
$versionNode.InnerText = "$ReleaseVersionNumber.$BuildNumber"
$vsixXml.Save($vsixFile)

# Make sure we don't have a release folder for this version already
if ((Get-Item $ReleaseFolder -ErrorAction SilentlyContinue) -ne $null)
{
	Write-Warning "$ReleaseFolder already exists on your local machine. It will now be deleted."
	Remove-Item $ReleaseFolder -Recurse
}

# Go get nuget.exe if we don't have it
$NuGet = "$BuildFolder\nuget.exe"
$FileExists = Test-Path $NuGet
If ($FileExists -eq $False) {
	$SourceNugetExe = "http://nuget.org/nuget.exe"
	Invoke-WebRequest $SourceNugetExe -OutFile $NuGet
}

# Restore packages (if they don't exist the build will fail)
$packagesTargetDirectory = "..\packages\"
Write-Host "Restoring NuGet packages, this may take a while depending on your package cache and connection speed"
.\nuget.exe install ..\Umbraco.ModelsBuilder\packages.config -OutputDirectory $packagesTargetDirectory -Verbosity quiet
.\nuget.exe install ..\Umbraco.ModelsBuilder.Console\packages.config -OutputDirectory $packagesTargetDirectory -Verbosity quiet
.\nuget.exe install ..\Umbraco.ModelsBuilder.CustomTool\packages.config -OutputDirectory $packagesTargetDirectory -Verbosity quiet
.\nuget.exe install ..\Umbraco.ModelsBuilder.Tests\packages.config -OutputDirectory $packagesTargetDirectory -Verbosity quiet

# Set the version number in SolutionInfo.cs
$SolutionInfoPath = Join-Path -Path $SolutionRoot -ChildPath "SolutionInfo.cs"
(gc -Path $SolutionInfoPath) `
	-replace "(?<=Version\(`")[.\d]*(?=`"\))", "$ReleaseVersionNumber.$BuildNumber" |
	sc -Path $SolutionInfoPath -Encoding UTF8
(gc -Path $SolutionInfoPath) `
	-replace "(?<=AssemblyInformationalVersion\(`")[.\w-]*(?=`"\))", "$ReleaseVersionNumber.$BuildNumber$PreReleaseName" |
	sc -Path $SolutionInfoPath -Encoding UTF8
# Set the copyright
$Copyright = "Copyright © Umbraco HQ " + (Get-Date).year;
(gc -Path $SolutionInfoPath) `
	-replace "(?<=AssemblyCopyright\(`").*(?=`"\))", $Copyright |
	sc -Path $SolutionInfoPath -Encoding UTF8;

# Build the solution in release mode
$SolutionPath = Join-Path -Path $SolutionRoot -ChildPath "Umbraco.ModelsBuilder.sln";

# clean sln for all deploys
& $MSBuild "$SolutionPath" /p:Configuration=Release /maxcpucount /t:Clean
if (-not $?)
{
	throw "The MSBuild process returned an error code."
}

#build
& $MSBuild "$SolutionPath" /p:Configuration=Release /maxcpucount
if (-not $?)
{
	throw "The MSBuild process returned an error code."
}

#prepare core
$CoreFolder = Join-Path -Path $ReleaseFolder -ChildPath "Umbraco.ModelsBuilder";
New-Item $CoreFolder -Type directory

$include = @('*Umbraco.ModelsBuilder.dll','*Umbraco.ModelsBuilder.pdb')
$CoreBinFolder = Join-Path -Path $SolutionRoot -ChildPath "Umbraco.ModelsBuilder\bin\Release";
Copy-Item "$CoreBinFolder\*.*" -Destination $CoreFolder -Include $include

$DashboardFiles = Join-Path -Path $SolutionRoot -ChildPath "Umbraco.ModelsBuilder\Dashboard"
Copy-Item "$DashboardFiles\*.js" -Destination $CoreFolder;
Copy-Item "$DashboardFiles\*.htm" -Destination $CoreFolder;
Copy-Item "$DashboardFiles\*.manifest" -Destination $CoreFolder;

#build core nuget
$CoreNuSpecSource = Join-Path -Path $BuildFolder -ChildPath "Nuspecs\ModelsBuilder\*";
Copy-Item $CoreNuSpecSource -Destination $CoreFolder
$CoreNuSpec = Join-Path -Path $CoreFolder -ChildPath "Umbraco.ModelsBuilder.nuspec";
& $NuGet pack $CoreNuSpec -OutputDirectory $ReleaseFolder -Version $ReleaseVersionNumber$PreReleaseName -Properties copyright=$Copyright

#prepare api
$ApiFolder = Join-Path -Path $ReleaseFolder -ChildPath "Umbraco.ModelsBuilder.Api";
New-Item $ApiFolder -Type directory

$include = @('*Umbraco.ModelsBuilder.Api.dll','*Umbraco.ModelsBuilder.Api.pdb')
$ApiBinFolder = Join-Path -Path $SolutionRoot -ChildPath "Umbraco.ModelsBuilder.Api\bin\Release";
Copy-Item "$ApiBinFolder\*.*" -Destination $ApiFolder -Include $include

#build api nuget
$ApiNuSpecSource = Join-Path -Path $BuildFolder -ChildPath "Nuspecs\ModelsBuilder.Api\*";
Copy-Item $ApiNuSpecSource -Destination $ApiFolder
$ApiNuSpec = Join-Path -Path $ApiFolder -ChildPath "Umbraco.ModelsBuilder.Api.nuspec";
& $NuGet pack $ApiNuSpec -OutputDirectory $ReleaseFolder -Version $ReleaseVersionNumber$PreReleaseName -Properties copyright=$Copyright

#copy vsix
Copy-Item "$SolutionRoot\Umbraco.ModelsBuilder.CustomTool\bin\Release\Umbraco.ModelsBuilder.CustomTool.vsix" -Destination "$ReleaseFolder\Umbraco.ModelsBuilder.CustomTool-$ReleaseVersionNumber$PreReleaseName.vsix"

""
"Build $ReleaseVersionNumber is done!"