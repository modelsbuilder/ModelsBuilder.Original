Write-Host "Get version"
$version = (Get-Command "..\src\Our.ModelsBuilder\bin\Release\Our.ModelsBuilder.dll").FileVersionInfo.ProductVersion
$version 
$nuspecs =  Get-ChildItem -Path ..\*\*.nuspec -Recurse -Force;
$nuspecs | Foreach-Object { 
    Write-Host "Set version for "$_.FullName 
    [xml]$xml = Get-Content -path  $_.FullName -Raw
    $ns = [System.Xml.XmlNamespaceManager]::new($xml.NameTable)
    $ns.AddNamespace('nuspec', 'http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd')
    
    write-host [xml]$xml
    $xml.package.metadata.version= $version.ToString()

    [xml]$xml.Save($_.FullName)


 }
