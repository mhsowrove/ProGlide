param([Parameter(Mandatory=$true)][string]$CompilerPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$release = Split-Path $root -Parent
& (Join-Path $PSScriptRoot 'build-local.ps1')
& (Join-Path $PSScriptRoot 'package-portable.ps1')
& $CompilerPath /Q ('/DPayloadDir=' + (Join-Path $release 'PodGlide-Portable')) ('/DReleaseDir=' + $release) (Join-Path $PSScriptRoot 'PodGlide.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
