[CmdletBinding()]
param(
    [string] $InstallPath = (Join-Path $env:LOCALAPPDATA 'ChatGPTWatchdog')
)

$ErrorActionPreference = 'Stop'

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

Get-Process -Name 'ChatGPTWatchdog' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

Remove-ItemProperty -Path $runKey -Name 'ChatGPTWatchdog' -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $installPath)
{
    Remove-Item -LiteralPath $installPath -Recurse -Force
}

Write-Output 'ChatGPT Watchdog uninstalled.'
