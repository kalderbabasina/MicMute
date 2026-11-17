# Publish your repository and downloads

1. Extract `MicMute-3.1.0-source.zip`. Commit the **contents** of its `MicMute` folder, including `.github`, `.gitignore`, `LICENSE`, and `THIRD-PARTY-NOTICES.md`. Do not commit locally generated settings, backups, logs, custom media, or build output.
2. Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test -Installer` locally. Check microphone selection, global hotkey, language changes, preset previews, local WAV/MP3, overlay behavior, and preference preservation on upgrade and uninstall. GitHub Actions runs the build and smoke tests on pushes and pull requests; check the first hosted run on the repository's **Actions** page.
3. Tag the exact commit used to build the binaries as `v3.1.0`. Create a draft release from that tag in **Releases → Draft a new release**. The workflow only uploads build artifacts; it does not create tags, publish releases, or deploy.
4. Attach `MicMute-portable.zip`, `MicMute-Setup.exe`, and the SHA-256 sums for the actual uploaded files as `MicMute-3.1.0-SHA256SUMS.txt`. Attach `MicMute-3.1.0-source.zip` (or clearly link the matching complete source, including build scripts). Keep the corresponding source available alongside the binaries, and preserve GPL-3.0-or-later licensing and attribution. Review the draft and its files before publishing manually.

When using CI outputs, download the files inside the Actions artifact wrappers, not the wrappers themselves. These executables are unsigned. No GitHub account data is required by the application.

## Source map

- `src/AudioController.cs`: Windows capture-device enumeration and direct endpoint mute access.
- `src/AppConfig.cs`: validated settings and atomic saves with a `.bak` copy.
- `src/Program.cs`: tray, global hotkey, status polling, and app lifetime.
- `src/OverlayForm.cs`: no-focus always-on-top window, alpha images, drag and resize.
- `src/VisualAssets.cs`: artwork and state-specific preset/custom media selection.
- `src/SettingsForm.cs`: language and preset selection, media previews, and editing preferences.
- `tests/SmokeTests.cs`: synthetic configuration/asset tests and read-only device probes. The optional `--verify-set-current` command checks direct mute writes using the value already set on each device.
- `installer/MicMute.iss` and `build.ps1`: per-user Windows installer and reproducible build.
