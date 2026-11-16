param([string]$OutputFolder = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'PodGlide-Portable'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force $OutputFolder | Out-Null
foreach ($name in @('PodGlide.exe','AirPodsSwitch.dll','AirPodsSwitch.runtimeconfig.json','THIRD-PARTY-NOTICES.md','LICENSE','PORTABLE-README.txt')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination $OutputFolder -Force
}
Copy-Item -LiteralPath (Join-Path $root 'Assets') -Destination $OutputFolder -Recurse -Force
Set-Content -LiteralPath (Join-Path $OutputFolder 'portable.flag') -Value 'Keep this file to store preferences in the local Data folder.'
$installed = Join-Path $env:ProgramFiles 'dotnet'
$runtime = Join-Path $OutputFolder 'Runtime'
New-Item -ItemType Directory -Force $runtime | Out-Null
foreach ($name in @('dotnet.exe','LICENSE.txt','ThirdPartyNotices.txt')) { Copy-Item -LiteralPath (Join-Path $installed $name) -Destination $runtime -Force }
foreach ($relative in @('host\fxr\8.0.32','shared\Microsoft.NETCore.App\8.0.32','shared\Microsoft.WindowsDesktop.App\8.0.32')) {
    $targetParent = Split-Path (Join-Path $runtime $relative) -Parent
    New-Item -ItemType Directory -Force $targetParent | Out-Null
    Copy-Item -LiteralPath (Join-Path $installed $relative) -Destination $targetParent -Recurse -Force
}
Write-Output ('Portable package: ' + $OutputFolder)
