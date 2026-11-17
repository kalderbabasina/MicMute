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
                Check(typeof(Program).Assembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
                    .Cast<System.Runtime.Versioning.TargetFrameworkAttribute>().Single().FrameworkName == ".NETFramework,Version=v4.8",
                    "csc and MSBuild target the same framework");
                foreach (string language in new[] { "en", "ru", "es" })
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
                    style.SelectedIndex = 1;
                    sounds.SelectedIndex = 2;
                    ui.ApplyLanguage("en");
                    Check(ui.Text == UiText.Get("MicMute — настройки", "en") &&
                        style.SelectedIndex == 1 && sounds.SelectedIndex == 2 && ui.Result.AppLanguage == "es",
                        "live language change preserves the unsaved draft and does not commit");
                }
                Check(UiText.Get("Настройки…", "en") != UiText.Get("Настройки…", "es") &&
                    UiText.Get("Настройки…", "ru") == "Настройки…", "tray strings localized");
                string soft = Path.Combine(temp, "soft.wav");
                byte[] softOn = AudioFeedback.Tone(1, false), softOff = AudioFeedback.Tone(1, true), arcade = AudioFeedback.Tone(2, false);
                File.WriteAllBytes(soft, softOn);
                Check(AudioFeedback.IsSoundFile(soft) && !softOn.SequenceEqual(softOff) && !softOn.SequenceEqual(arcade), "distinct local WAV presets");
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
    }
}
