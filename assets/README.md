# Default artwork

`default/mic_on.ico` and `default/mic_off.ico` are original microphone glyphs drawn by `VisualAssets.DrawStatus` in this repository, distributed under the project GPL-3.0-or-later license. The same glyphs are built into the application, so missing files do not prevent startup.

To regenerate these files after building with `-Test`:

```powershell
.\dist\MicMute.SmokeTests.exe --export-icons .\assets\default
```

The Classic, Outline, and Pixel variants are built into the app. Custom files are selected in Settings; keep them in a stable location such as `assets/custom`, outside the packaged default artwork directory. If a file becomes missing or unreadable, MicMute uses the selected preset for that state.
