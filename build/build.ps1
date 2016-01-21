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

# Make sure we don't have a release folder for this version already
if ((Get-Item $ReleaseFolder -ErrorAction SilentlyContinue) -ne $null)
{
	Write-Warning "$ReleaseFolder already exists on your local machine. It will now be deleted."
	Remove-Item $ReleaseFolder -Recurse
}

# Go get nuget.exe if we don't hae it
$NuGet = "$BuildFolder\nuget.exe"
$FileExists = Test-Path $NuGet 
If ($FileExists -eq $False) {
	$SourceNugetExe = "http://nuget.org/nuget.exe"
	Invoke-WebRequest $SourceNugetExe -OutFile $NuGet
}

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

$CoreFolder = Join-Path -Path $ReleaseFolder -ChildPath "Umbraco.ModelsBuilder";
$AspNetFolder = Join-Path -Path $ReleaseFolder -ChildPath "Umbraco.ModelsBuilder.AspNet";

New-Item $CoreFolder -Type directory
New-Item $AspNetFolder -Type directory

$include = @('*Umbraco.ModelsBuilder.dll','*Umbraco.ModelsBuilder.pdb')
$CoreBinFolder = Join-Path -Path $SolutionRoot -ChildPath "Umbraco.ModelsBuilder\bin\Release";
Copy-Item "$CoreBinFolder\*.*" -Destination $CoreFolder -Include $include

$include = @('*Umbraco.ModelsBuilder.AspNet.dll','*Umbraco.ModelsBuilder.AspNet.pdb')
$AspNetBinFolder = Join-Path -Path $SolutionRoot -ChildPath "Umbraco.ModelsBuilder.AspNet\bin\Release";
Copy-Item "$AspNetBinFolder\*.*" -Destination $AspNetFolder -Include $include

#build core nuget
$CoreNuSpecSource = Join-Path -Path $BuildFolder -ChildPath "Nuspecs\ModelsBuilder\*";
Copy-Item $CoreNuSpecSource -Destination $CoreFolder
$CoreNuSpec = Join-Path -Path $CoreFolder -ChildPath "Umbraco.ModelsBuilder.nuspec";
& $NuGet pack $CoreNuSpec -OutputDirectory $ReleaseFolder -Version $ReleaseVersionNumber$PreReleaseName -Properties copyright=$Copyright

#build aspnet nuget
# - first need to copy over some files
$DashboardFiles = Join-Path -Path $SolutionRoot -ChildPath "Umbraco.ModelsBuilder.AspNet\Dashboard"
Copy-Item "$DashboardFiles\*.js" -Destination $AspNetFolder;
Copy-Item "$DashboardFiles\*.htm" -Destination $AspNetFolder;
Copy-Item "$DashboardFiles\*.manifest" -Destination $AspNetFolder;
$AspNetNuSpecSource = Join-Path -Path $BuildFolder -ChildPath "Nuspecs\ModelsBuilder.AspNet\*";
Copy-Item $AspNetNuSpecSource -Destination $AspNetFolder
$AspNetNuSpec = Join-Path -Path $AspNetFolder -ChildPath "Umbraco.ModelsBuilder.AspNet.nuspec";
& $NuGet pack $AspNetNuSpec -OutputDirectory $ReleaseFolder -Version $ReleaseVersionNumber$PreReleaseName -Properties copyright=$Copyright


""
"Build $ReleaseVersionNumber is done!"