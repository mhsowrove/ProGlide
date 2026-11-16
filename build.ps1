param([switch]$SelfContained)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $contained = if ($SelfContained) { 'true' } else { 'false' }
    dotnet publish .\AirPodsSwitch.csproj -c Release -r win-x64 --self-contained $contained -p:PublishSingleFile=true -o .\publish
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
} finally { Pop-Location }
