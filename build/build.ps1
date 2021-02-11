Write-Host "Starting rebuild of Project"
Write-Host "Still need figure out way to build correctly"
#dotnet build ..\src\Our.ModelsBuilder.sln
Write-Host "Setting Up Version"

& ".\SetVersion.ps1"

Write-Host "Creating Pacakges"
$nuspecs =  Get-ChildItem -Path ..\*\*.nuspec -Recurse -Force;
$nuspecs | Foreach-Object { 
    nuget pack $_.FullName -OutputDirectory "NugetPackages"
 }

 Write-Host "Removing Version"
 & ".\UnSetVersion.ps1"

