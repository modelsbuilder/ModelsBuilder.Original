
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

  Write-Host "ZpqrtBnk.ModelsBuilder Build"
  Write-Host "Umbraco.Build v$($ubuild.BuildVersion)"

  # ################################################################
  # TASKS
  # ################################################################

  $ubuild.DefineMethod("ClearBuild",
  {
	if (test-path $this.BuildTemp) { remove-item $this.BuildTemp -force -recurse -errorAction SilentlyContinue > $null }
	if (test-path $this.BuildOutput) { remove-item $this.BuildOutput -force -recurse -errorAction SilentlyContinue > $null }

	mkdir $this.BuildTemp > $null
	mkdir $this.BuildOutput > $null
  })

  $ubuild.DefineMethod("SetMoreUmbracoVersion",
  {
    param ( $semver )

    # Edit VSIX
    Write-Host "Update VSIX manifest."
    $vsixFile = "$($this.SolutionRoot)\src\ZpqrtBnk.ModelsBuilder.Extension\source.extension.vsixmanifest"
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
    &$this.BuildEnv.NuGet restore "$($this.SolutionRoot)\src\ZpqrtBnk.ModelsBuilder.sln" > "$($this.BuildTemp)\nuget.restore.log"
    # temp - ignore errors, because of a circular dependency between U and MB
    #   we'll eventually move ZpqrtBnk.ModelsBuilder (and only that one) into Core,
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
    &$this.BuildEnv.VisualStudio.MsBuild "$src\ZpqrtBnk.ModelsBuilder.sln" `
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
    Write-Host "Package ZpqrtBnk.ModelsBuilder"
    $nuspecs = "$($this.SolutionRoot)\build\NuSpecs"
    $copyright = "Copyright (C) ZpqrtBnk $((Get-Date).Year)"
	  &$this.BuildEnv.NuGet pack "$nuspecs\ZpqrtBnk.ModelsBuilder.nuspec" `
	    -Properties copyright="$Copyright"`;solution="$($this.SolutionRoot)" `
	    -Version "$($this.Version.Semver.ToString())" `
	    -Verbosity detailed -OutputDirectory "$($this.BuildOutput)" > "$($this.BuildTemp)\nupack.core.log"
  	if (-not $?) { throw "Failed to pack NuGet ZpqrtBnk.ModelsBuilder." }
  })

  $ubuild.DefineMethod("PackageWeb",
  {
    Write-Host "Package ZpqrtBnk.ModelsBuilder.Web"
    $nuspecs = "$($this.SolutionRoot)\build\NuSpecs"
    $copyright = "Copyright (C) ZpqrtBnk $((Get-Date).Year)"
	  &$this.BuildEnv.NuGet pack "$nuspecs\ZpqrtBnk.ModelsBuilder.Web.nuspec" `
	    -Properties copyright="$Copyright"`;solution="$($this.SolutionRoot)" `
	    -Version "$($this.Version.Semver.ToString())" `
	    -Verbosity detailed -OutputDirectory "$($this.BuildOutput)" > "$($this.BuildTemp)\nupack.web.log"
  	if (-not $?) { throw "Failed to pack NuGet ZpqrtBnk.ModelsBuilder.Web." }
  })

  $ubuild.DefineMethod("PackageVsix",
  {
    Write-Host "Package ZpqrtBnk.ModelsBuilder.Extension"

	$vsix = "$($this.SolutionRoot)\build.tmp\bin\ZpqrtBnk.ModelsBuilder.Extension.vsix"
	$temp = "$($this.SolutionRoot)\build.tmp\bin\ZpqrtBnk.ModelsBuilder.Extension.temp"
	$target = "$($this.BuildOutput)\ZpqrtBnk.ModelsBuilder.Extension-$($this.Version.Semver.ToString()).vsix"

	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
	[System.IO.Compression.ZipFile]::ExtractToDirectory($vsix, $temp) | Out-Null

	Remove-Item -Force -Recurse "$temp/x86"
	Remove-Item -Force -Recurse "$temp/amd64"
	Remove-Item -Force -Recurse "$temp/cs"
	Remove-Item -Force -Recurse "$temp/de"
	Remove-Item -Force -Recurse "$temp/es"
	Remove-Item -Force -Recurse "$temp/fr"
	Remove-Item -Force -Recurse "$temp/it"
	Remove-Item -Force -Recurse "$temp/ja"
	Remove-Item -Force -Recurse "$temp/ko"
	Remove-Item -Force -Recurse "$temp/pl"
	Remove-Item -Force -Recurse "$temp/pt-BR"
	Remove-Item -Force -Recurse "$temp/ru"
	Remove-Item -Force -Recurse "$temp/tr"
	Remove-Item -Force -Recurse "$temp/zh-Hans"
	Remove-Item -Force -Recurse "$temp/zh-Hant"

	Remove-Item -Force "$temp/ClientDependency.*"
	Remove-Item -Force "$temp/CSharpTest.*"
	Remove-Item -Force "$temp/HtmlAgilityPack.*"
	Remove-Item -Force "$temp/ImageProcessor.*"
	Remove-Item -Force "$temp/LightInject.*"
	Remove-Item -Force "$temp/Lucene.*"
	Remove-Item -Force "$temp/Markdown.*"
	Remove-Item -Force "$temp/MiniProfiler.*"
	Remove-Item -Force "$temp/NPoco.*"
	Remove-Item -Force "$temp/Serilog.*"
	Remove-Item -Force "$temp/Superpower.*"
	Remove-Item -Force "$temp/Umbraco.Examine.*"

	Remove-Item -Force "$temp/*.pdb"

	$manifest = Get-Content "$temp/manifest.json" | ConvertFrom-Json

	$files = $manifest.files # is an array of objects (fixed size)
	$nfiles = @()

	foreach ($file in $files)
	{
		$fileName = $file.fileName

		if ($fileName.StartsWith("/x86")) { continue }
		if ($fileName.StartsWith("/amd64")) { continue }
		if ($fileName.StartsWith("/cs")) { continue }
		if ($fileName.StartsWith("/de")) { continue }
		if ($fileName.StartsWith("/es")) { continue }
		if ($fileName.StartsWith("/fr")) { continue }
		if ($fileName.StartsWith("/it")) { continue }
		if ($fileName.StartsWith("/ja")) { continue }
		if ($fileName.StartsWith("/ko")) { continue }
		if ($fileName.StartsWith("/pl")) { continue }
		if ($fileName.StartsWith("/pt-BR")) { continue }
		if ($fileName.StartsWith("/tr")) { continue }
		if ($fileName.StartsWith("/zh-Hans")) { continue }
		if ($fileName.StartsWith("/zh-Hant")) { continue }

		if ($fileName.StartsWith("/ClientDependency.")) { continue }
		if ($fileName.StartsWith("/CSharpTest.")) { continue }
		if ($fileName.StartsWith("/HtmlAgilityPack.")) { continue }
		if ($fileName.StartsWith("/ImageProcessor.")) { continue }
		if ($fileName.StartsWith("/LightInject.")) { continue }
		if ($fileName.StartsWith("/Lucene.")) { continue }
		if ($fileName.StartsWith("/Markdown.")) { continue }
		if ($fileName.StartsWith("/MiniProfiler.")) { continue }
		if ($fileName.StartsWith("/NPoco.")) { continue }
		if ($fileName.StartsWith("/Serilog.")) { continue }
		if ($fileName.StartsWith("/Superpower.")) { continue }
		if ($fileName.StartsWith("/Umbraco.Examine.")) { continue }

		if ($fileName.EndsWith(".pdb")) { continue }

		$nfiles += $file
	}

	$manifest.files = $nfiles

	$manifest | ConvertTo-Json | Set-Content "$temp/manifest.json"

	[System.IO.Compression.ZipFile]::CreateFromDirectory($temp, $target)
  })

  $ubuild.DefineMethod("VerifyNuGet",
  {
    $this.VerifyNuGetConsistency(
      ("ZpqrtBnk.ModelsBuilder", "ZpqrtBnk.ModelsBuilder.Web"),
      ("ZpqrtBnk.ModelsBuilder", "ZpqrtBnk.ModelsBuilder.Web", "ZpqrtBnk.ModelsBuilder.Extension", "ZpqrtBnk.ModelsBuilder.Console"))
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
    $this.PackageWeb()
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
