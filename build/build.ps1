
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
    [switch] $continue = $false
  )

  # ################################################################
  # BOOTSTRAP
  # ################################################################

  # create and boot the buildsystem
  $ubuild = &"$PSScriptRoot\build-bootstrap.ps1"
  if (-not $?) { return }
  $ubuild.Boot($PSScriptRoot, 
    @{ Local = $local; With7Zip = $false; WithNode = $false },
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
    $vsixFile = "$($this.SolutionRoot)\src\Umbraco.ModelsBuilder.CustomTool\source.extension.vsixmanifest"
    [xml] $vsixXml = Get-Content $vsixFile
    $xmlNameTable = New-Object System.Xml.NameTable
    $xmlNameSpace = New-Object System.Xml.XmlNamespaceManager($xmlNameTable)
    $xmlNameSpace.AddNamespace("vsx", "http://schemas.microsoft.com/developer/vsx-schema/2011")
    $xmlNameSpace.AddNamespace("d", "http://schemas.microsoft.com/developer/vsx-schema-design/2011")
    $versionNode = $vsixXml.SelectSingleNode("/vsx:PackageManifest/vsx:Metadata/vsx:Identity/@Version", $xmlNameSpace)
	  #$versionNode.InnerText = "$ReleaseVersionNumber.$BuildNumber"
	  $versionNode.InnerText = "$semver.Release"
    $vsixXml.Save($vsixFile)
  })

  $ubuild.DefineMethod("RestoreNuGet",
  {
    Write-Host "Restore NuGet"
    Write-Host "Logging to $($this.BuildTemp)\nuget.restore.log"
    &$this.BuildEnv.NuGet restore "$($this.SolutionRoot)\src\Umbraco.ModelsBuilder.sln" -ConfigFile $this.BuildEnv.NuGetConfig > "$($this.BuildTemp)\nuget.restore.log"
    if (-not $?) { throw "Failed to restore NuGet packages." }   
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
      /p:Platform=AnyCPU `
      /p:UseWPP_CopyWebApplication=True `
      /p:PipelineDependsOnBuild=False `
      /p:OutDir="$($this.BuildTemp)\bin\\" `
      /p:WebProjectOutputDir="$($this.BuildTemp)\WebApp\\" `
      /p:Verbosity=minimal `
      /t:Clean`;Rebuild `
      /tv:"$($ubuild.BuildEnv.VisualStudio.ToolsVersion)" `
      /p:UmbracoBuild=True `
      > $log

    if (-not $?) { throw "Failed to compile." }
    
    # /p:UmbracoBuild tells the csproj that we are building from PS, not VS
  })

  $ubuild.DefineMethod("PackageCore",
  {
    $nuspecs = "$($this.SolutionRoot)\build\NuSpecs"
    $copyright = "Copyright © Umbraco $((Get-Date).Year)"
	  &$this.BuildEnv.NuGet pack "$nuspecs\Umbraco.ModelsBuilder.nuspec" `
	    -Properties copyright=$Copyright solution="$($this.SolutionRoot)" `
	    -Version "$($this.Version.Semver.ToString())" `
	    -Verbosity quiet -OutputDirectory "$($this.BuildOutput)"
  	if (-not $?) { throw "Failed to pack NuGet Umbraco.ModelsBuilder." }
  })

  $ubuild.DefineMethod("PackageApi",
  {
    $nuspecs = "$($this.SolutionRoot)\build\NuSpecs"
    $copyright = "Copyright © Umbraco $((Get-Date).Year)"
	  &$this.BuildEnv.NuGet pack "$nuspecs\Umbraco.ModelsBuilder.Api.nuspec" `
      -Properties copyright=$Copyright solution="$($this.SolutionRoot)" `
	    -Version "$($this.Version.Semver.ToString())" `
	    -Verbosity quiet -OutputDirectory "$($this.BuildOutput)"
  	if (-not $?) { throw "Failed to pack NuGet Umbraco.ModelsBuilder.Api." }
  })

  $ubuild.DefineMethod("PackageVsix",
  {
    #copy vsix
  	$this.CopyFile("$($this.SolutionRoot)\build.tmp\Umbraco.ModelsBuilder.CustomTool.vsix",
	    "$($this.BuildOutput)\Umbraco.ModelsBuilder.CustomTool-$($this.Version.Semver.ToString()).vsix")
  })

  $ubuild.DefineMethod("VerifyNuGet",
  {
    $this.VerifyNuGetConsistency(
      ("Umbraco.ModelsBuilder", "Umbraco.ModelsBuilder.Api"),
      ("Umbraco.ModelsBuilder", "Umbraco.ModelsBuilder.Api", "Umbraco.ModelsBuilder.CustomTool", "Umbraco.ModelsBuilder.Console"))      
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
    $this.PackageVsix()
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
    $ubuild.Build() 
    if ($ubuild.OnError()) { return }
  }
  Write-Host "Done"
  if ($get) { return $ubuild }
