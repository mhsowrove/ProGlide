param([switch]$NoStartup)
$ErrorActionPreference = 'Stop'
$destination = Join-Path $env:LOCALAPPDATA 'Programs\AirPodsSwitch'
New-Item -ItemType Directory -Force $destination | Out-Null
Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -and $_.ExecutablePath.StartsWith($destination + '\',[StringComparison]::OrdinalIgnoreCase) -and $_.Name -in @('dotnet.exe','ProGlide.exe','PodGlide.exe') } | ForEach-Object { Stop-Process -Id $_.ProcessId }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ProGlide.exe') -Destination $destination -Force
$exe = Join-Path $destination 'ProGlide.exe'
if (!$NoStartup) { $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Microsoft\Windows\CurrentVersion\Run'); try { $key.SetValue('AirPodsSwitch', ('"' + $exe + '" --startup')) } finally { $key.Dispose() } }
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'ProGlide.lnk'))
$shortcut.TargetPath = $exe; $shortcut.IconLocation = $exe; $shortcut.WorkingDirectory = $destination; $shortcut.Description = 'ProGlide · Your sound, one tap away'; $shortcut.Save()
Start-Process -FilePath $exe -WorkingDirectory $destination -WindowStyle Hidden
Write-Output ('Installed compact app: ' + $exe)
