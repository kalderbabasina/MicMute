# Publish your repository and downloads

1. Commit the public source files, including `.github`, `.gitignore`, `LICENSE`, and `THIRD-PARTY-NOTICES.md`. Do not commit locally generated settings, backups, logs, custom media, or build output.
2. Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test -Installer` locally. Check microphone selection, global hotkey, language changes, preset previews, local WAV/MP3, overlay behavior, sign-in startup, and preference preservation on upgrade and uninstall. GitHub Actions runs the build and smoke tests on pushes and pull requests.
3. Tag the exact commit used to build the binaries with the matching version (e.g. `v3.2.0`). Create a draft release from that tag in **Releases → Draft a new release**. The workflow only uploads build artifacts; it does not create tags, publish releases, or deploy.
4. Attach `MicMute-portable.zip`, `MicMute-Setup.exe`, and SHA-256 sums for the actual uploaded files. Attach a versioned source ZIP or clearly link the matching complete source, including build scripts. Preserve GPL-3.0-or-later licensing and attribution. Review the draft and its files before publishing manually.

When using CI outputs, download the files inside the Actions artifact wrappers, not the wrappers themselves. These executables are unsigned. No GitHub account data is required by the application.

The in-app check reads public stable releases tagged `vMAJOR.MINOR.PATCH`. Keep the installer asset named `MicMute-Setup.exe` with its GitHub SHA-256 digest; without it, the app offers the release page only. To test the live feed and installer bytes without running Setup, run `dist\MicMute.SmokeTests.exe --verify-update-feed` after `build.ps1 -Test`.

## Source map

- `src/AudioController.cs`: Windows capture-device enumeration and direct endpoint mute access.
- `src/AppConfig.cs`: validated settings and atomic saves with a `.bak` copy.
- `src/Program.cs`: tray, global hotkey, status polling, and app lifetime.
- `src/StartupShortcut.cs`: per-user Windows Startup shortcut management.
- `src/GitHubUpdates.cs`: public GitHub release lookup, installer verification, and Windows attachment policy.
- `src/OverlayForm.cs`: no-focus always-on-top window, alpha images, drag and resize.
- `src/VisualAssets.cs`: artwork and state-specific preset/custom media selection.
- `src/SettingsForm.cs`: language and preset selection, media previews, and editing preferences.
- `tests/SmokeTests.cs`: synthetic configuration/asset tests and read-only device probes. The optional `--verify-set-current` command checks direct mute writes using the value already set on each device.
- `installer/MicMute.iss` and `build.ps1`: per-user Windows installer and reproducible build.
