# Alternative build used for this delivery when no SDK was available.
# Runtime implementation assemblies are used instead of SDK reference assemblies.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$shared = Join-Path $env:ProgramFiles 'dotnet\shared'
$core = Get-ChildItem (Join-Path $shared 'Microsoft.NETCore.App') -Directory | Where-Object Name -Like '8.0.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$desktop = Get-ChildItem (Join-Path $shared 'Microsoft.WindowsDesktop.App') -Directory | Where-Object Name -Like '8.0.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$core -or !$desktop -or !(Test-Path $compiler)) { throw 'Requires .NET 8 Desktop Runtime x64 and the Windows .NET Framework compiler. Use build.ps1 with an SDK instead.' }
$references = Get-ChildItem $core.FullName,$desktop.FullName -Filter *.dll | Where-Object {
    try { $null = [Reflection.AssemblyName]::GetAssemblyName($_.FullName); $true } catch { $false }
} | ForEach-Object { '/r:"' + $_.FullName + '"' }
$response = Join-Path $env:TEMP ('AirPodsSwitch-' + [guid]::NewGuid() + '.rsp')
try {
    $lines = @('/nologo','/target:winexe','/platform:x64','/nostdlib+',('/out:"' + (Join-Path $root 'AirPodsSwitch.dll') + '"'),('/win32manifest:"' + (Join-Path $root 'app.manifest') + '"')) + $references
    $lines += @('Audio.cs','Program.cs','Diagnostics.cs','Theme.cs','About.cs') | ForEach-Object { '"' + (Join-Path $root $_) + '"' }
    $lines += '/win32icon:"' + (Join-Path $root 'Assets\PodGlide.ico') + '"'
    $lines | Set-Content $response -Encoding UTF8
    & $compiler /noconfig ('@' + $response)
    if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed. Exit the tray app before rebuilding.' }
    & $compiler /nologo /target:winexe /platform:x64 /r:System.Windows.Forms.dll ('/win32icon:' + (Join-Path $root 'Assets\PodGlide.ico')) ('/out:' + (Join-Path $root 'PodGlide.exe')) (Join-Path $PSScriptRoot 'Launcher.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }
} finally { Remove-Item -LiteralPath $response -ErrorAction SilentlyContinue }
