using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MicMute
{
    internal static class SmokeTests
    {
        private static int checks;
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + name);
            checks++;
        }

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--verify-update-feed")
            {
                var release = GitHubUpdates.CheckAsync(new Version(0, 0, 0), System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                Check(release != null && release.InstallerUrl != null, "public release has a verifiable installer");
                var bytes = GitHubUpdates.DownloadAsync(release, System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                string folder = Path.Combine(Path.GetTempPath(), "MicMute-feed-test-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                string installer = Path.Combine(folder, "MicMute-Setup.exe");
                try
                {
                    File.WriteAllBytes(installer, bytes);
                    GitHubUpdates.ApplyAttachmentPolicy(installer, release.InstallerUrl);
                    Check(GitHubUpdates.MatchesDigest(File.ReadAllBytes(installer), release.Digest, release.Size), "installer unchanged after Windows policy");
                    Console.WriteLine("Verified GitHub release v" + release.Version + ", installer SHA-256, and Windows attachment policy; Setup was not run.");
                    return 0;
                }
                finally { File.Delete(installer); Directory.Delete(folder); }
            }
            if (args.Length == 2 && args[0] == "--verify-mp3")
            {
                using (var player = new AudioFeedback(new AppConfig()))
                {
                    Check(player.TryPlayMp3(args[1]), "Windows accepts MP3 for playback");
                    Check(player.IsPlayingMp3, "MP3 decoder reports playback");
                    var elapsed = System.Diagnostics.Stopwatch.StartNew();
                    while (elapsed.ElapsedMilliseconds < 2400)
                    {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(25);
                    }
                    Check(!player.IsPlayingMp3, "short MP3 playback finished");
                    using (File.Open(args[1], FileMode.Open, FileAccess.Read, FileShare.None)) { }
                    Console.WriteLine("MP3 decode, playback, and file release verified.");
                    return 0;
                }
            }
            if (args.Length == 2 && args[0] == "--export-icons")
            {
                Directory.CreateDirectory(args[1]);
                using (Icon icon = VisualAssets.CreateStatusIcon(false))
                using (var file = File.Create(Path.Combine(args[1], "mic_on.ico"))) icon.Save(file);
                using (Icon icon = VisualAssets.CreateStatusIcon(true))
                using (var file = File.Create(Path.Combine(args[1], "mic_off.ico"))) icon.Save(file);
                return 0;
            }
            if (args.Length == 1 && args[0] == "--verify-set-current")
            {
                int verified = 0;
                foreach (AudioDeviceInfo device in AudioController.GetAvailableMicrophones())
                {
                    bool? current = AudioController.GetMuteState(device.Id);
                    if (!current.HasValue) continue;
                    // Idempotent integration probe: write the state already present; never toggle.
                    if (!AudioController.SetMuteState(device.Id, current.Value) || AudioController.GetMuteState(device.Id) != current)
                        throw new InvalidOperationException("Endpoint did not confirm its unchanged mute state.");
                    verified++;
                }
                Console.WriteLine("Verified SetMute with unchanged state on " + verified + " capture endpoints.");
                return verified > 0 ? 0 : 2;
            }
            string temp = Path.Combine(Path.GetTempPath(), "MicMute-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                string path = Path.Combine(temp, "MicMuteConfig.txt");
                AppConfig config = AppConfig.Load(path);
                Check(config.OverlayEnabled && config.HotkeyKey == Keys.F13, "defaults");
                Check(config.AppLanguage == "en" && config.IconStyle == 0 && config.SoundStyle == 0, "release defaults");
                Check(config.CheckUpdatesOnStartup && config.MuteSource == SoundSource.Style, "startup check and built-in sound defaults");
                TestReleaseMetadata();
                Check(typeof(Program).Assembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
                    .Cast<System.Runtime.Versioning.TargetFrameworkAttribute>().Single().FrameworkName == ".NETFramework,Version=v4.8",
                    "csc and MSBuild target the same framework");
                foreach (string language in new[] { "en", "ru", "es", "uk-surzhyk" })
                {
                    config.AppLanguage = language; config.IconStyle = 2; config.SoundStyle = 1;
                    config.Save(path);
                    var restored = AppConfig.Load(path);
                    Check(restored.AppLanguage == language && restored.IconStyle == 2 && restored.SoundStyle == 1, "language and presets roundtrip: " + language);
                    using (var ui = new SettingsForm(restored))
                    {
                        Check(ui.Result.AppLanguage == language && ui.Controls.Find("Language", true).Length == 1, "settings language selector: " + language);
                    }
                }
                using (var ui = new SettingsForm(config))
                {
                    var style = (ComboBox)ui.Controls.Find("IconStyle", true).Single();
                    var sounds = (ComboBox)ui.Controls.Find("SoundStyle", true).Single();
                    var mute = (ComboBox)ui.Controls.Find("MuteSource", true).Single();
                    style.SelectedIndex = 1;
                    sounds.SelectedIndex = 2;
                    mute.SelectedIndex = (int)SoundSource.Warning;
                    ui.ApplyLanguage("en");
                    Check(ui.Text == UiText.Get("MicMute — настройки", "en") &&
                        style.SelectedIndex == 1 && sounds.SelectedIndex == 2 && ui.Result.AppLanguage == "uk-surzhyk",
                        "live language change preserves the unsaved draft and does not commit");
                    Check(mute.SelectedIndex == (int)SoundSource.Warning && mute.Text == "Windows — Warning",
                        "sound selection survives live language switch");
                }
                using (var ui = new SettingsForm(config, true))
                    Check(ui.StartAtSignIn && ((CheckBox)ui.Controls.Find("StartAtSignIn", true).Single()).Text ==
                        UiText.Get("Запускать при входе в Windows", "uk-surzhyk"), "startup checkbox reflects actual registration and language");
                using (var ui = new SettingsForm(config, null))
                    Check(!ui.StartAtSignIn && !ui.Controls.Find("StartAtSignIn", true).Single().Enabled,
                        "startup checkbox is unavailable when another instance owns the shortcut");
                Check(UiText.Get("Настройки…", "en") != UiText.Get("Настройки…", "es") &&
                    UiText.Get("Настройки…", "ru") == "Настройки…", "tray strings localized");
                Check(UiText.Get("Микрофон выключен", "uk-surzhyk") != "Микрофон выключен" &&
                    UiText.Get("Микрофон выключен", "uk-surzhyk") != UiText.Get("Микрофон включён", "uk-surzhyk"),
                    "Surzhyk distinguishes microphone states");
                Check(!Program.IsStartupLaunch(new string[0]) &&
                    !Program.IsStartupLaunch(new[] { "--startup-other" }) &&
                    Program.IsStartupLaunch(new[] { "--startup" }), "only explicit startup launches are quiet");
                string shortcut = Path.Combine(temp, "MicMute.lnk");
                string exe = Application.ExecutablePath;
                Check(StartupShortcut.GetState(shortcut, exe) == false, "missing startup is off");
                StartupShortcut.SetEnabled(shortcut, exe, true);
                Check(StartupShortcut.GetState(shortcut, exe) == true, "create native startup shortcut");
                Check(StartupShortcut.GetState(shortcut, Path.Combine(temp, "Other MicMute.exe")) == null,
                    "another installation owns the shortcut");
                bool startupProtected = false;
                try { StartupShortcut.SetEnabled(shortcut, Path.Combine(temp, "Other MicMute.exe"), false); }
                catch (InvalidOperationException) { startupProtected = true; }
                Check(startupProtected && StartupShortcut.GetState(shortcut, exe) == true,
                    "cannot disable another installation's startup");
                StartupShortcut.SetEnabled(shortcut, exe, false);
                Check(!File.Exists(shortcut), "remove only the owned startup shortcut");
                string soft = Path.Combine(temp, "soft.wav");
                byte[] softOn = AudioFeedback.Tone(1, false), softOff = AudioFeedback.Tone(1, true), arcade = AudioFeedback.Tone(2, false);
                File.WriteAllBytes(soft, softOn);
                Check(AudioFeedback.IsSoundFile(soft) && !softOn.SequenceEqual(softOff) && !softOn.SequenceEqual(arcade), "distinct local WAV presets");
                string legacyPath = Path.Combine(temp, "legacy.txt");
                File.WriteAllText(legacyPath, "MUTE_SOUND=" + soft + "\nUNMUTE_SOURCE=99");
                var legacy = AppConfig.Load(legacyPath);
                Check(legacy.MuteSource == SoundSource.Custom && legacy.UnmuteSource == SoundSource.Style, "old paths select custom, invalid source uses style");
                legacy.MuteSource = SoundSource.Error;
                legacy.UnmuteSource = SoundSource.Silent;
                legacy.CheckUpdatesOnStartup = false;
                legacy.Save(legacyPath);
                var savedSounds = AppConfig.Load(legacyPath);
                Check(savedSounds.MuteSource == SoundSource.Error && savedSounds.UnmuteSource == SoundSource.Silent &&
                    savedSounds.MuteSound == soft && !savedSounds.CheckUpdatesOnStartup, "independent sources and startup check roundtrip without losing the custom path");
                Check(AudioFeedback.WindowsSound(SoundSource.Warning, false) == System.Media.SystemSounds.Exclamation &&
                    AudioFeedback.WindowsSound(SoundSource.Information, true) == System.Media.SystemSounds.Asterisk &&
                    AudioFeedback.WindowsSound(SoundSource.Style, true) == System.Media.SystemSounds.Hand, "native Windows signal mapping");
                string badMp3 = Path.Combine(temp, "invalid.mp3");
                File.WriteAllText(badMp3, "this is not an mp3 file");
                Check(!AudioFeedback.IsSoundFile(badMp3), "reject fake MP3");
                config.MicrophoneDeviceId = "synthetic-capture-id";
                config.HotkeyModifiers = Keys.Control | Keys.Shift;
                config.OverlaySize = 200;
                config.TrayOnIcon = "C:\\synthetic\\icon=on.ico";
                config.Save(path);
                AppConfig loaded = AppConfig.Load(path);
                Check(loaded.MicrophoneDeviceId == config.MicrophoneDeviceId && loaded.TrayOnIcon == config.TrayOnIcon, "config values with equals roundtrip");
                Check(loaded.HotkeyModifiers == config.HotkeyModifiers && loaded.OverlaySize == 200, "hotkey/size roundtrip");
                loaded.OverlaySize = 9999; loaded.OverlayOpacity = -1;
                Check(loaded.OverlaySize == 256 && loaded.OverlayOpacity == 25, "bounds");
                loaded.Save(path);
                Check(File.Exists(path + ".bak") && AppConfig.Load(path + ".bak").OverlaySize == 200, "atomic save keeps previous config");
                loaded.HotkeyEnabled = false; loaded.HotkeyKey = Keys.None;
                loaded.Save(path);
                Check(AppConfig.Load(path).HotkeyKey == Keys.None, "disabled cleared hotkey");
                string before = File.ReadAllText(path);
                loaded.MuteSound = "bad\nSOUNDS_ENABLED=false";
                bool rejected = false;
                try { loaded.Save(path); } catch (ArgumentException) { rejected = true; }
                Check(rejected && File.ReadAllText(path) == before, "newline rejection preserves saved file");
                File.WriteAllText(path, "OVERLAY_SIZE=bad\nHOTKEY_KEY=99999\nHOTKEY_MODIFIERS=Windows\nOVERLAY_X=-256\nSOUNDS_ENABLED=false\nUNKNOWN_KEY=ignored\n", Encoding.UTF8);
                loaded = AppConfig.Load(path);
                Check(loaded.OverlaySize == 64 && loaded.HotkeyKey == Keys.F13 && loaded.HotkeyModifiers == Keys.None, "malformed individual keys keep defaults");
                Check(loaded.OverlayX == -256 && !loaded.SoundsEnabled, "valid keys survive malformed lines");
                AppConfig clone = loaded.Clone(); clone.OverlaySize = 42;
                Check(loaded.OverlaySize == 64, "dialog config clone is isolated");
                using (var settings = new SettingsForm(loaded))
                    Check(settings.Result != loaded && !settings.ResetOverlayPosition, "settings constructor clones without applying changes");

                Check(VisualAssets.LoadImage(Path.Combine(temp, "absent.png")) == null, "missing image fallback");
                Check(VisualAssets.LoadIcon(Path.Combine(temp, "absent.ico")) == null, "missing icon fallback");
                Check(VisualAssets.LoadIcon("bad::icon.ico") == null && VisualAssets.LoadImage("bad::image.png") == null, "malformed path fallback");
                string invalid = Path.Combine(temp, "invalid.png"); File.WriteAllText(invalid, "not an image");
                Check(VisualAssets.LoadImage(invalid) == null, "corrupt image fallback");
                string png = Path.Combine(temp, "sample.png");
                using (var bitmap = new Bitmap(32, 24)) { bitmap.SetPixel(3, 4, Color.Red); bitmap.Save(png, System.Drawing.Imaging.ImageFormat.Png); }
                using (Image image = VisualAssets.LoadImage(png))
                {
                    File.Move(png, png + ".moved");
                    Check(image != null && image.Width == 32 && image.Height == 24, "decoded image is not file-locked");
                }
                string oversize = Path.Combine(temp, "large.png");
                using (var bitmap = new Bitmap(4097, 2)) bitmap.Save(oversize, System.Drawing.Imaging.ImageFormat.Png);
                Check(VisualAssets.LoadImage(oversize) == null, "image dimension limit");
                File.Move(png + ".moved", png);
                using (var ui = new SettingsForm(new AppConfig { OverlayOnImage = png, OverlayOffImage = invalid }))
                {
                    var overlayPreview = (PictureBox)ui.Controls.Find("OverlayActivePreview", true).Single();
                    var fallback = (PictureBox)ui.Controls.Find("OverlayMutedPreview", true).Single();
                    var trayPreview = (PictureBox)ui.Controls.Find("TrayActivePreview", true).Single();
                    Check(overlayPreview.Image.Width == 64 && overlayPreview.Image.Height == 48 &&
                        fallback.Image.Width == fallback.Image.Height && trayPreview.Image != null,
                        "artwork previews show custom aspect ratio and built-in fallback");
                }
                using (Icon classic = VisualAssets.CreateStatusIcon(true, 0))
                using (Icon outline = VisualAssets.CreateStatusIcon(true, 1))
                using (Icon pixel = VisualAssets.CreateStatusIcon(true, 2))
                using (Bitmap a = classic.ToBitmap())
                using (Bitmap b = outline.ToBitmap())
                using (Bitmap c = pixel.ToBitmap())
                    Check(a.Width == 64 && a.GetPixel(12, 12) != b.GetPixel(12, 12) && b.GetPixel(12, 12) != c.GetPixel(12, 12), "three distinct 64px icon styles");
                using (var visuals = new VisualAssets(new AppConfig { OverlayOnImage = invalid, TrayOffIcon = "missing.ico" }))
                    Check(visuals.ActiveIcon != null && visuals.MutedIcon != null && visuals.UnknownIcon != null && visuals.ActiveImage == null, "safe asset fallbacks");

                // Always read-only: CI runners may legitimately have no audio service/device.
                try
                {
                    var devices = AudioController.GetAvailableMicrophones();
                    Check(devices != null, "audio enumeration");
                    Console.WriteLine("Active capture devices: " + devices.Count);
                    foreach (var device in devices) Check(!string.IsNullOrEmpty(device.Id), "capture device ID present");
                }
                catch (InvalidOperationException) { Console.WriteLine("Audio service unavailable: read-only device probe skipped."); }
                Check(!AudioController.GetMuteState("synthetic-invalid-device").HasValue, "missing explicit device has no fallback");
                Check(!AudioController.SetMuteState("synthetic-invalid-device", true), "invalid target cannot silently succeed");
                Console.WriteLine("PASS: " + checks + " checks; no real microphone state was changed.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally { Directory.Delete(temp, true); }
        }
        private static void TestReleaseMetadata()
        {
            const string digest = "sha256:ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
            const string url = "https://github.com/kalderbabasina/MicMute/releases/download/v3.9.0/MicMute-Setup.exe";
            string json = "{\"tag_name\":\"v3.9.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"MicMute-Setup.exe\"," +
                "\"state\":\"uploaded\",\"size\":3,\"digest\":\"" + digest + "\",\"browser_download_url\":\"" + url + "\"}]}";
            var current = new Version(3, 2, 0);
            Func<string, GitHubUpdates.Release> parse = text => GitHubUpdates.Parse(Encoding.UTF8.GetBytes(text), current);
            var release = parse(json);
            Check(release.Version == new Version(3, 9, 0) && release.InstallerUrl == url && release.Digest == digest,
                "stable official release and installer identified");
            Check(GitHubUpdates.Parse(Encoding.UTF8.GetBytes(json), release.Version) == null &&
                GitHubUpdates.Parse(Encoding.UTF8.GetBytes(json), new Version(4, 0, 0)) == null &&
                parse(json.Replace("\"prerelease\":false", "\"prerelease\":true")) == null &&
                parse(json.Replace("\"draft\":false", "\"draft\":true")) == null,
                "skip equal, older, prerelease and draft versions");
            Check(parse(json.Replace(url, "https://example.invalid/MicMute-Setup.exe")).InstallerUrl == null &&
                parse(json.Replace(digest, "invalid")).InstallerUrl == null &&
                parse(json.Replace("\"size\":3", "\"size\":16777217")).InstallerUrl == null,
                "untrusted installer metadata falls back to the release page");
            Check(GitHubUpdates.MatchesDigest(Encoding.ASCII.GetBytes("abc"), digest, 3) &&
                !GitHubUpdates.MatchesDigest(Encoding.ASCII.GetBytes("xyz"), digest, 3) &&
                !GitHubUpdates.MatchesDigest(Encoding.ASCII.GetBytes("abc"), digest, 4), "reject mismatched hash or size");
            Check(GitHubUpdates.IsDownloadRedirect(new Uri("https://release-assets.githubusercontent.com/github-production-release-asset/item")) &&
                !GitHubUpdates.IsDownloadRedirect(new Uri("http://release-assets.githubusercontent.com/file")) &&
                !GitHubUpdates.IsDownloadRedirect(new Uri("https://release-assets.githubusercontent.com.example.invalid/file")),
                "installer redirects are HTTPS and restricted to the GitHub asset host");
        }
    }
}
