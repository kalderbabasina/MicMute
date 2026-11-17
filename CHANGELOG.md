# Changelog

## 3.1.0

- English is the default interface language; Russian and Spanish are selectable in Settings.
- Classic, Outline, and Pixel icon styles, with a built-in preview for both states and separate custom icons and images.
- Windows, Soft, and Arcade sound styles, independent mute/unmute previews, and local WAV and MP3 overrides without NuGet packages.
- The tray, hotkey, device selection, and movable, resizable overlay remain available. Existing settings and custom media are not removed by uninstall.
- English, Russian, and Spanish documentation and optional Russian and Spanish installer messages from Inno Setup. CI builds packages only; releases are published separately.

## 3.0.0 — 2026-11-17

- Open-source C# 5 / WinForms application for .NET Framework 4.8, retaining the `MicMute2.exe` executable name.
- Configurable global hotkey, tray controls, and movable, lockable on-screen microphone status.
- Default communications microphone or a selected capture device, with explicit unavailable-state feedback.
- Custom tray icons, overlay images, and WAV feedback with safe built-in fallbacks.
- Overlay size and opacity controls; first-run settings and local preference backup.
- Allowlisted portable package, per-user Inno Setup installer, and build-only Windows CI.
- English and Russian instructions and GPL attribution.

This source package does not publish a GitHub release, upload personal settings, or deploy an application.
