
  param (
    # get, don't execute
    [Parameter(Mandatory=$false)]
    [Alias("g")]
    [switch] $get = $false,

    # run local, don't download, assume everything is ready
    [Parameter(Mandatory=$false)]
    [Alias("l")]
    [Alias("loc")]
    [switch] $local = $false,

    # keep the build directories, don't clear them
    [Parameter(Mandatory=$false)]
    [Alias("c")]
    [Alias("cont")]
    [switch] $continue = $false,

    # execute a command
    [Parameter(Mandatory=$false, ValueFromRemainingArguments=$true)]
    [String[]]
    $command
  )

  # ################################################################
  # BOOTSTRAP
  # ################################################################

  # create and boot the buildsystem
  $ubuild = &"$PSScriptRoot\build-bootstrap.ps1"
  if (-not $?) { return }
  $ubuild.Boot($PSScriptRoot,
    @{ Local = $local; With7Zip = $false; WithNode = $false; VsMajor = "15"; VsPreview = $false; },
    @{ Continue = $continue })
  if ($ubuild.OnError()) { return }

  Write-Host "Zbu.ModelsBuilder Build"
  Write-Host "Umbraco.Build v$($ubuild.BuildVersion)"

  # ################################################################
  # TASKS
  # ################################################################

  $ubuild.DefineMethod("SetMoreUmbracoVersion",
  {
    param ( $semver )

    # Edit VSIX
    Write-Host "Update VSIX manifest."
    $vsixFile = "$($this.SolutionRoot)\src\Umbraco.ModelsBuilder.CustomTool\source.extension.vsixmanifest"
    [xml] $vsixXml = Get-Content $vsixFile
    $xmlNameTable = New-Object System.Xml.NameTable
    $xmlNameSpace = New-Object System.Xml.XmlNamespaceManager($xmlNameTable)
    $xmlNameSpace.AddNamespace("vsx", "http://schemas.microsoft.com/developer/vsx-schema/2011")
    $xmlNameSpace.AddNamespace("d", "http://schemas.microsoft.com/developer/vsx-schema-design/2011")
    $versionNode = $vsixXml.SelectSingleNode("/vsx:PackageManifest/vsx:Metadata/vsx:Identity/@Version", $xmlNameSpace)

    # cannot be semver because it has to be a.b.c.d format
    # so we have to invent some sort of "build" - the spaghetti way
    $current = $versionNode.Value
    $pos = $current.LastIndexOf('.')
    $current = $current.Substring($pos + 1)
    $now = [DateTime]::Now.ToString("yyMMdd")
    if ($current.Length -ne 9)
    {
      $build = $now + "001"
    }
    else
    {
      if (-not $current.StartsWith($now))
      {
        $build = $now + "001"
      }
      else
      {
        $i = 0
        if ([int]::TryParse($current.Substring(6), [ref]$i))
        {
          $i += 1
          $build = $now + $i.ToString("000")
        }
        else
        {
          $build = $now + "666"
        }
      }
    }

    $release = "" + $semver.Major + "." + $semver.Minor + "." + $semver.Patch
	  $versionNode.Value = "$release.$build"
    $vsixXml.Save($vsixFile)
  })

  $ubuild.DefineMethod("RestoreNuGet",
  {
    Write-Host "Restore NuGet"
    Write-Host "Logging to $($this.BuildTemp)\nuget.restore.log"
    &$this.BuildEnv.NuGet restore "$($this.SolutionRoot)\src\Umbraco.ModelsBuilder.sln" > "$($this.BuildTemp)\nuget.restore.log"
    # temp - ignore errors, because of a circular dependency between U and MB
    #   we'll eventually move Umbraco.ModelsBuilder (and only that one) into Core,
    #   once I have decided what to do with the work-in-progress stuff
    #if (-not $?) { throw "Failed to restore NuGet packages." }
    $error.Clear()
  })

  $ubuild.DefineMethod("Compile",
  {
    $buildConfiguration = "Release"

    $src = "$($this.SolutionRoot)\src"
    $log = "$($this.BuildTemp)\msbuild.log"

    if ($this.BuildEnv.VisualStudio -eq $null)
    {
      throw "Build environment does not provide VisualStudio."
    }

    Write-Host "Compile"
    Write-Host "Logging to $log"

    # beware of the weird double \\ at the end of paths
    # see http://edgylogic.com/blog/powershell-and-external-commands-done-right/
    &$this.BuildEnv.VisualStudio.MsBuild "$src\Umbraco.ModelsBuilder.sln" `
      /p:WarningLevel=0 `
      /p:Configuration=$buildConfiguration `
      /p:Platform="Any CPU" `
      /p:UseWPP_CopyWebApplication=True `
      /p:PipelineDependsOnBuild=False `
      /p:OutDir="$($this.BuildTemp)\bin\\" `
      /p:Verbosity=minimal `
      /t:Clean`;Rebuild `
      /tv:"$($this.BuildEnv.VisualStudio.ToolsVersion)" `
      /p:UmbracoBuild=True `
      > $log

    if (-not $?) { throw "Failed to compile." }

    # /p:UmbracoBuild tells the csproj that we are building from PS, not VS
  })

  $ubuild.DefineMethod("PackageCore",
  {
    Write-Host "Package Umbraco.ModelsBuilder"
    $nuspecs = "$($this.SolutionRoot)\build\NuSpecs"
    $copyright = "Copyright (C) Umbraco $((Get-Date).Year)"
	  &$this.BuildEnv.NuGet pack "$nuspecs\Umbraco.ModelsBuilder.nuspec" `
	    -Properties copyright="$Copyright"`;solution="$($this.SolutionRoot)" `
	    -Version "$($this.Version.Semver.ToString())" `
	    -Verbosity detailed -OutputDirectory "$($this.BuildOutput)" > "$($this.BuildTemp)\nupack.core.log"
  	if (-not $?) { throw "Failed to pack NuGet Umbraco.ModelsBuilder." }
  })

  $ubuild.DefineMethod("PackageUi",
  {
    Write-Host "Package Umbraco.ModelsBuilder.Ui"
    $nuspecs = "$($this.SolutionRoot)\build\NuSpecs"
    $copyright = "Copyright (C) Umbraco $((Get-Date).Year)"
	  &$this.BuildEnv.NuGet pack "$nuspecs\Umbraco.ModelsBuilder.Ui.nuspec" `
	    -Properties copyright="$Copyright"`;solution="$($this.SolutionRoot)" `
	    -Version "$($this.Version.Semver.ToString())" `
	    -Verbosity detailed -OutputDirectory "$($this.BuildOutput)" > "$($this.BuildTemp)\nupack.ui.log"
  	if (-not $?) { throw "Failed to pack NuGet Umbraco.ModelsBuilder.Ui." }
  })

  $ubuild.DefineMethod("PackageApi",
  {
    Write-Host "Package Umbraco.ModelsBuilder.Api"
    $nuspecs = "$($this.SolutionRoot)\build\NuSpecs"
    $copyright = "Copyright (C) Umbraco $((Get-Date).Year)"
	  &$this.BuildEnv.NuGet pack "$nuspecs\Umbraco.ModelsBuilder.Api.nuspec" `
      -Properties copyright="$Copyright"`;solution="$($this.SolutionRoot)" `
	    -Version "$($this.Version.Semver.ToString())" `
	    -Verbosity detailed -OutputDirectory "$($this.BuildOutput)" > "$($this.BuildTemp)\nupack.api.log"
  	if (-not $?) { throw "Failed to pack NuGet Umbraco.ModelsBuilder.Api." }
  })

  $ubuild.DefineMethod("PackageVsix",
  {
    Write-Host "Package Umbraco.ModelsBuilder.CustomTool"
  	$this.CopyFile("$($this.SolutionRoot)\build.tmp\bin\Umbraco.ModelsBuilder.CustomTool.vsix",
	    "$($this.BuildOutput)\Umbraco.ModelsBuilder.CustomTool-$($this.Version.Semver.ToString()).vsix")
  })

  $ubuild.DefineMethod("VerifyNuGet",
  {
    $this.VerifyNuGetConsistency(
      ("Umbraco.ModelsBuilder", "Umbraco.ModelsBuilder.Api"),
      ("Umbraco.ModelsBuilder", "Umbraco.ModelsBuilder.Api", "Umbraco.ModelsBuilder.CustomTool", "Umbraco.ModelsBuilder.Console"))
  })

  $ubuild.DefineMethod("PostPackageHook",
  {
    # run hook
    if ($this.HasMethod("PostPackage"))
    {
      Write-Host "Run PostPackage hook"
      $this.PostPackage();
      if (-not $?) { throw "Failed to run hook." }
    }
  })

  $ubuild.DefineMethod("Build",
  {
    $this.RestoreNuGet()
    if ($this.OnError()) { return }
    $this.Compile()
    if ($this.OnError()) { return }
    #$this.CompileTests()
    # not running tests
    $this.VerifyNuGet()
    if ($this.OnError()) { return }
    $this.PackageCore()
    if ($this.OnError()) { return }
    $this.PackageApi()
    if ($this.OnError()) { return }
    $this.PackageUi()
    if ($this.OnError()) { return }
    $this.PackageVsix()
    if ($this.OnError()) { return }
    $this.PostPackageHook()
    if ($this.OnError()) { return }
  })

  # ################################################################
  # RUN
  # ################################################################

  # configure
  $ubuild.ReleaseBranches = @( "master" )

  # run
  if (-not $get)
  {
    if ($command.Length -eq 0)
    {
      $command = @( "Build" )
    }
    $ubuild.RunMethod($command);
    if ($ubuild.OnError()) { return }
  }
  if ($get) { return $ubuild }
