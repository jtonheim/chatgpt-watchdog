[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $projectRoot 'src\ChatGPTWatchdog\ChatGPTWatchdog.csproj'
$publishPath = Join-Path $projectRoot 'artifacts\publish'
$installPath = Join-Path $env:LOCALAPPDATA 'ChatGPTWatchdog'
$installedExe = Join-Path $installPath 'ChatGPTWatchdog.exe'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$legacyShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'ChatGPT.lnk'

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    --output $publishPath

Get-Process -Name 'ChatGPTWatchdog' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $installPath -Force | Out-Null
Copy-Item -Path (Join-Path $publishPath '*') -Destination $installPath -Recurse -Force

Set-ItemProperty -Path $runKey -Name 'ChatGPTWatchdog' -Value "`"$installedExe`""

if (Test-Path -LiteralPath $legacyShortcut)
{
    Remove-Item -LiteralPath $legacyShortcut -Force
}

Start-Process -FilePath $installedExe

Write-Output "ChatGPT Watchdog installed to $installPath"

