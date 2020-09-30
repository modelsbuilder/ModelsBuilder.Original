$nuspecs =  Get-ChildItem -Path ..\*\*.nuspec -Recurse -Force;
$nuspecs | Foreach-Object { 
    [xml]$xml = Get-Content -path  $_.FullName -Raw
    $ns = [System.Xml.XmlNamespaceManager]::new($xml.NameTable)
    $ns.AddNamespace('nuspec', 'http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd')
    $xml.package.metadata.version= ""
    [xml]$xml.Save($_.FullName)
 }