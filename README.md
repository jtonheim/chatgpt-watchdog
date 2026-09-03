# ChatGPT Watchdog

A small Windows system-tray application that keeps the ChatGPT desktop app running.

The watchdog checks for the `ChatGPT` process every 15 seconds. If monitoring is enabled and ChatGPT is not running, it launches the packaged desktop app through its version-independent Windows app identity. A 30-second cooldown prevents repeated launch attempts while ChatGPT starts or updates.

## Tray menu

- **Keep ChatGPT running** enables or disables monitoring without closing ChatGPT.
- **Start watchdog with Windows** controls the current user's Windows startup entry.
- **Start ChatGPT now** requests an immediate launch.
- **Open settings and log folder** opens `%LOCALAPPDATA%\ChatGPTWatchdog`.
- **Exit watchdog** closes the tray application without changing its startup setting.

Double-clicking the tray icon starts ChatGPT.

## Install

This project targets .NET 10 for Windows. From PowerShell:

```powershell
.\scripts\install.ps1
```

The installer publishes the application to `%LOCALAPPDATA%\ChatGPTWatchdog`, enables it at sign-in under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, removes the older direct `ChatGPT.lnk` startup shortcut if present, and starts the watchdog.

## Uninstall

```powershell
.\scripts\uninstall.ps1
```

## Build

```powershell
dotnet build .\ChatGPTWatchdog.slnx --configuration Release
```

Settings and logs are stored locally under `%LOCALAPPDATA%\ChatGPTWatchdog` and are not sent anywhere.

