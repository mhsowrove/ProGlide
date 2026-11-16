param([string]$OutputFolder = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'ProGlide-Compact-Portable'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force $OutputFolder | Out-Null
foreach ($name in @('ProGlide.exe','THIRD-PARTY-NOTICES.md','LICENSE','PORTABLE-README.txt','ARTWORK-PROVENANCE.md')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination $OutputFolder -Force
}
Copy-Item -LiteralPath (Join-Path $root 'Assets\PodBattery-LICENSE.txt') -Destination $OutputFolder -Force
Set-Content -LiteralPath (Join-Path $OutputFolder 'portable.flag') -Value 'Keep this file to store preferences in the local Data folder.'
Write-Output ('Compact portable package: ' + $OutputFolder)
