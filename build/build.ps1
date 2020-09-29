dotnet build ..\src\Our.ModelsBuilder.sln
$version = (Get-Command "..\src\Our.ModelsBuilder\bin\Release\Our.ModelsBuilder.dll").FileVersionInfo.ProductVersion
$version 
$nuspecs =  Get-ChildItem -Path ..\*\*.nuspec -Recurse -Force;
$nuspecs | Foreach-Object { 
    [xml]$xml = Get-Content -path  $_.FullName -Raw
    $ns = [System.Xml.XmlNamespaceManager]::new($xml.NameTable)
    $ns.AddNamespace('nuspec', 'http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd')
    
    write-host [xml]$xml
    $xml.package.metadata.version= $version.ToString()

    [xml]$xml.Save($_.FullName)
    nuget pack $_.FullName -OutputDirectory "NugetPackages"
    $xml.package.metadata.version= ""
    [xml]$xml.Save($_.FullName)

 }
