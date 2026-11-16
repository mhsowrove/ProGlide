# Build using the compiler and Windows APIs already included with Windows 11.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'BuildSupport\build-local.ps1')
