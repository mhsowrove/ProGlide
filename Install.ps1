param([switch]$NoStartup)
$ErrorActionPreference = 'Stop'
$destination = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\AirPodsSwitch'
$preferencesFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AirPodsSwitch'
New-Item -ItemType Directory -Force $destination,$preferencesFolder | Out-Null
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($destination) -and $_.CommandLine.Contains('AirPodsSwitch.dll') } | ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction Stop }
foreach ($name in @('PodGlide.exe','AirPodsSwitch.dll','AirPodsSwitch.runtimeconfig.json')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $destination $name) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Assets') -Destination $destination -Recurse -Force
$settingsFile = Join-Path $preferencesFolder 'settings.xml'
if (!(Test-Path -LiteralPath $settingsFile)) {
    $report = Join-Path $env:TEMP ('AirPodsSwitch-' + [guid]::NewGuid() + '.txt')
    try {
        & (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe') (Join-Path $destination 'AirPodsSwitch.dll') --diagnose --report $report
        if ($LASTEXITCODE -ne 0) { throw 'Audio diagnostics failed; install the .NET 8 Desktop Runtime x64 and try again.' }
        $line = Get-Content $report | Where-Object { $_ -match '^Default role 1: ' } | Select-Object -First 1
        $currentId = if ($line) { $line.Substring(16).Trim() } else { '' }
        $deviceLine = Get-Content $report | Where-Object { $_ -like ('* | ' + $currentId) } | Select-Object -First 1
        if ($currentId -eq 'none' -or $deviceLine -like '*AirPods*') { $currentId = '' }
        $xml = New-Object System.Xml.XmlDocument
        $null = $xml.AppendChild($xml.CreateXmlDeclaration('1.0','utf-8',$null))
        $root = $xml.CreateElement('Preferences'); $null = $xml.AppendChild($root)
        foreach ($entry in @(@('AirPodsId',''),@('SpeakersId',$currentId),@('Match','AirPods'),@('AutoSwitch','false'))) {
            $element = $xml.CreateElement($entry[0]); $element.InnerText = $entry[1]; $null = $root.AppendChild($element)
        }
        $xml.Save($settingsFile)
    } finally { Remove-Item -LiteralPath $report -ErrorAction SilentlyContinue }
}
$exe = Join-Path $destination 'PodGlide.exe'
if (!$NoStartup) {
    $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
    try { $key.SetValue('AirPodsSwitch', ('"' + $exe + '" --startup')) } finally { $key.Dispose() }
}
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'PodGlide.lnk'))
$shortcut.TargetPath = $exe; $shortcut.IconLocation = (Join-Path $destination 'Assets\PodGlide.ico'); $shortcut.WorkingDirectory = $destination; $shortcut.Description = 'PodGlide · Your sound, one tap away'; $shortcut.Save()
$legacyShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'AirPods Switch.lnk'
if (Test-Path -LiteralPath $legacyShortcut) {
    $legacy = $shell.CreateShortcut($legacyShortcut)
    if ($legacy.TargetPath -eq (Join-Path $destination 'AirPodsSwitch.exe')) { Remove-Item -LiteralPath $legacyShortcut }
}
Start-Process -FilePath $exe -WorkingDirectory $destination -WindowStyle Hidden
Write-Output ('Installed and launched: ' + $exe)
Write-Output ('Preferences: ' + $settingsFile)
