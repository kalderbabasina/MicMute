<h1 align="center">MicMute</h1>

<p align="center">Your microphone. One shortcut. A clear status on screen.</p>

<p align="center">
  <a href="https://github.com/kalderbabasina/MicMute/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/kalderbabasina/MicMute?color=238636&amp;style=flat-square"></a>
  <a href="https://github.com/kalderbabasina/MicMute/actions/workflows/build.yml"><img alt="Windows build" src="https://github.com/kalderbabasina/MicMute/actions/workflows/build.yml/badge.svg"></a>
  <a href="LICENSE"><img alt="License: GPL-3.0-or-later" src="https://img.shields.io/badge/license-GPL--3.0--or--later-blue?style=flat-square"></a>
</p>

<p align="center">
  <a href="https://github.com/kalderbabasina/MicMute/releases/latest">Download</a> ·
  <a href="README.ru.md">Русский</a> ·
  <a href="README.es.md">Español</a> ·
  <a href="https://github.com/kalderbabasina/MicMute/issues">Report an issue</a>
</p>

A small, customizable Windows microphone mute controller. Built with **C# / WinForms / .NET Framework 4.8**, using native Windows APIs and **no NuGet packages**.

- **Mute from anywhere:** a configurable global hotkey, tray controls, or a click on the overlay.
- **Keep status in view:** a movable always-on-top indicator with Shift-drag resizing, opacity, and a position lock.
- **Choose your microphone:** follow the Windows communications default or pin a specific capture device.
- **Make it yours:** Classic, Outline, and Pixel icons; Windows, Soft, and Arcade sounds; your own images, ICO, WAV, and MP3 files.
- **Keep it local:** English, Russian, and Spanish. No recording, telemetry, account, or online service.

## Download

| Package | Best for |
| --- | --- |
| [Windows installer](https://github.com/kalderbabasina/MicMute/releases/latest/download/MicMute-Setup.exe) | Per-user installation, optional desktop and startup shortcuts |
| [Portable ZIP](https://github.com/kalderbabasina/MicMute/releases/latest/download/MicMute-portable.zip) | Extract into a writable folder and run `MicMute2.exe` |

[All releases, source archives, and SHA-256 checksums](https://github.com/kalderbabasina/MicMute/releases)

## Quick start

1. Install or extract MicMute and launch it. Settings open on the first run.
2. Select your microphone and a convenient global shortcut; the default is **F13**.
3. Click the on-screen indicator to toggle mute. Drag to move it; **Shift-drag** to resize.

Green means active, red means muted, and a gray **?** means the device state is unknown. Right-click the tray icon or indicator to reopen Settings.

## Install or run portably

Requires **Windows 10 or newer** with **.NET Framework 4.8 or newer**.

- Installer: run `MicMute-Setup.exe`. It installs for the current user without administrator rights. Desktop and sign-in startup shortcuts are optional and unchecked by default.
- Portable: extract `MicMute-portable.zip` into a writable folder and run `MicMute2.exe`. Do not run it from inside the ZIP.

These builds are unsigned. Windows may show a reputation warning for newly downloaded executables. Compare release files with the published SHA-256 checksums.

Settings open on the first run. The app defaults to English; select English, Русский, or Español under **General → Language**. Preferences are stored in `MicMuteConfig.txt` beside the executable; an atomic-save backup uses `.bak`. Keep this file and custom media private. Uninstall removes installed program files but leaves locally created settings and custom files. Keep custom media in a separate folder such as `assets/custom`, not in the installer-managed `assets/default` folder.

## Use

- Select the Windows **default communications microphone** or a specific active capture device in Settings. The default selection follows Windows device changes; a specific selection stays tied to that device.
- Toggle mute with the global hotkey, tray icon, or overlay. The default hotkey is **F13**; configure another key and Ctrl/Alt/Shift modifiers in Settings. A shortcut already registered by another application cannot be used.
- Right-click the tray icon to open its menu and Settings. Show/hide or lock the overlay from the available controls.
- Drag the overlay to move it; **Shift-drag** to resize it from **40–256 px**. Set opacity from **25–100%** in Settings. Locking prevents movement and resizing, but still allows click-to-toggle.
- A gray **?** means the selected microphone is unavailable or its state could not be read. It does not mean “muted.” Check device connection, Windows microphone permissions, and the selected device.

MicMute changes the endpoint mute state, not an application's separate mute button. Another application or Windows can change the same endpoint state; the indicator follows the observed state.

## Custom appearance and sounds

Choose **Classic**, **Outline**, or **Pixel** under **Appearance → Icon style**. The settings show both built-in states. Choose **Windows**, **Soft**, or **Arcade** under **Sounds → Sound style**. **Preview mute** and **Preview unmute** play the corresponding feedback without changing microphone mute.

You can override the preset separately for the muted and active states:

| Item | Supported files |
| --- | --- |
| Tray icons | `.ico` |
| Overlay images | `.png`, `.bmp`, `.jpg`, `.jpeg`, `.ico` |
| Feedback sounds | `.wav`, `.mp3` |

Use transparent images for a clean overlay. Each media file is limited to 16 MB, and raster images to 4096 pixels per dimension. If a custom file cannot be used, the corresponding preset supplies the fallback. The Windows sound style uses Hand for mute and Asterisk for unmute; these depend on your Windows sound scheme. MP3 playback uses Windows MCI for local files and closes the playback device when the track ends; no additional package or codec is bundled. Custom media are not included in public packages.

## Privacy and Windows limitations

MicMute does **not record audio**, send network requests, collect telemetry, or include an updater. It reads capture-device mute state and changes it when requested. Preferences contain device identifiers and local file paths, so do not publish your local configuration or backups.

The indicator is an ordinary always-on-top desktop window. It cannot guarantee visibility over exclusive-fullscreen applications, UAC's secure desktop, or protected windows. Global shortcuts can also be intercepted or restricted by other software. MicMute is not a security boundary or a hardware microphone disconnect.

## Build from source

Use Windows PowerShell **5.1** and .NET Framework **4.8**. The script invokes the Windows Framework `csc.exe` directly; a .NET SDK and Visual Studio are not required.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

To build the installer, install **Inno Setup 6 or 7**. English is always included; Russian and Spanish installer messages are included when the corresponding compiler translations are available:

```powershell
.\build.ps1 -Test -Installer
.\build.ps1 -Installer -InnoCompiler "C:\Tools\Inno Setup 6\ISCC.exe"
```

Outputs are `dist/portable/MicMute2.exe`, `dist/MicMute-portable.zip`, and, when requested, `dist/installer/MicMute-Setup.exe`. The portable ZIP is created from fresh allowlisted staging, not from an existing installation: only the executable, bundled default icons, license, READMEs, and attribution notices are included. The build does not recursively clean `dist` or remove local configuration. The old-style `MicMute.csproj` is also available for MSBuild/Visual Studio with the .NET Framework 4.8 targeting pack.

GitHub Actions builds and tests Windows packages and uploads build artifacts only. It does not publish releases or deploy anything. Smoke tests do not replace manual microphone, hotkey, overlay, device-change, and installer checks on Windows.

## License and credits

**GPL-3.0-or-later**; see [LICENSE](LICENSE). This project continues work from **AveYo** and **rjcncpt**, with upstream at [rjcncpt/micmute](https://github.com/rjcncpt/micmute). See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for attribution and [CHANGELOG.md](CHANGELOG.md) for this version's scope. Bundled default icons are generated for this project; bring your own media only when you have permission to use it.

## Feedback and contributing

[Bug reports and small, focused pull requests](https://github.com/kalderbabasina/MicMute/issues) are welcome. Include your Windows version, MicMute version, expected behavior, and steps to reproduce. Do not attach your configuration, device IDs, private paths, or personal media. For a code change, run `build.ps1 -Test` and describe any manual Windows checks you performed. See [PUBLISHING.md](PUBLISHING.md) for the source map and release process.
