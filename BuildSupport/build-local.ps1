# Compact Windows build: uses the .NET Framework included with Windows 11.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$response = Join-Path $env:TEMP ('ProGlide-' + [guid]::NewGuid() + '.rsp')
try {
    $lines = @('/nologo','/target:winexe','/platform:x64',('/out:"' + (Join-Path $root 'ProGlide.exe') + '"'),('/win32manifest:"' + (Join-Path $root 'app.manifest') + '"'),('/win32icon:"' + (Join-Path $root 'Assets\PodGlide.ico') + '"'))
    foreach ($reference in @('System.dll','System.Core.dll','System.Xml.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')) {
        $lines += '/r:"' + (Join-Path $framework $reference) + '"'
    }
    foreach ($reference in @('System.Runtime.dll','System.Runtime.WindowsRuntime.dll','System.Runtime.InteropServices.WindowsRuntime.dll')) { $lines += '/r:"' + (Join-Path $framework $reference) + '"' }
    $metadataRoot = Join-Path $env:WINDIR 'System32\WinMetadata'
    if (Test-Path (Join-Path $metadataRoot 'Windows.Devices.winmd')) {
        foreach ($metadata in @('Windows.Foundation.winmd','Windows.Devices.winmd','Windows.Storage.winmd')) { $lines += '/r:"' + (Join-Path $metadataRoot $metadata) + '"' }
    } else {
        $unionRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\UnionMetadata'
        $union = Get-ChildItem $unionRoot -Filter Windows.winmd -Recurse -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
        if (!$union) { throw 'Windows Bluetooth API build metadata is absent. Build on Windows 11 or install the Windows SDK.' }
        $lines += '/r:"' + $union.FullName + '"'
    }
    $lines += @('Audio.cs','Program.cs','Diagnostics.cs','Theme.cs','About.cs','AirPodsCase.cs','Battery.cs','AssemblyInfo.cs') | ForEach-Object { '"' + (Join-Path $root $_) + '"' }
    foreach ($asset in (Get-ChildItem (Join-Path $root 'Assets') -File | Where-Object { $_.Extension -in '.png','.jpg','.ico','.txt' })) { $lines += '/resource:"' + $asset.FullName + '",ProGlide.Assets.' + $asset.Name }
    $lines | Set-Content $response -Encoding UTF8
    & $compiler /noconfig ('@' + $response)
    if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed. Exit the app before rebuilding.' }
} finally { Remove-Item -LiteralPath $response -ErrorAction SilentlyContinue }
