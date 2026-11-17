# Attribution and third-party notices

## Original project

MicMute continues the microphone-muting work of **AveYo** and **rjcncpt**. This continuation is distributed under **GPL-3.0-or-later**; see [LICENSE](LICENSE). The [upstream v2.1.1 license file](https://github.com/rjcncpt/micmute/blob/3ed5ef37e42905c904fbb999e9049b42de870bb3/LICENSE) contains the MIT notice reproduced below, although the [upstream README at the same tag](https://github.com/rjcncpt/micmute/blob/3ed5ef37e42905c904fbb999e9049b42de870bb3/README.md#license) describes upstream as GPL-3.0. This notice retains the upstream attribution and license text as published.

```text
MIT License

Copyright (c) [2026] [René Jansen]

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Windows components

The application uses Windows Core Audio, Windows MCI for local MP3 playback, Win32 APIs, WinForms, and .NET Framework 4.8. Those platform components are provided by Microsoft and are not redistributed with this source package. The Windows sound style uses the user's system-sound scheme; no Windows audio files are bundled.

## Build tooling and assets

[Inno Setup](https://jrsoftware.org/isinfo.php), by Jordan Russell and Martijn Laan, builds the optional installer. The installer uses language messages supplied with Inno Setup, including Russian and Spanish when available. The Inno Setup compiler and its source files are not bundled. GitHub Actions uses checkout and artifact-upload actions only for CI.

The built-in microphone styles and synthesized Soft and Arcade sounds are original to this project and covered by its GPL-3.0-or-later license. No third-party NuGet libraries, fonts, sound packs, or themes are bundled. User-selected media remain subject to their respective owners' licenses and are excluded from distributed packages.
