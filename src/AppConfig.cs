using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MicMute
{
    public enum SoundSource { Style, Custom, Error, Information, Warning, Question, Beep, Silent }

    public sealed class AppConfig
    {
        private int overlaySize = 64;
        private int overlayOpacity = 100;
        private int iconStyle;
        private int soundStyle;
        private SoundSource? muteSource, unmuteSource;

        public string AppLanguage { get; set; }
        public int IconStyle { get { return iconStyle; } set { iconStyle = Math.Max(0, Math.Min(2, value)); } }
        public int SoundStyle { get { return soundStyle; } set { soundStyle = Math.Max(0, Math.Min(2, value)); } }
        public string MicrophoneDeviceId { get; set; }
        public bool HotkeyEnabled { get; set; }
        public Keys HotkeyKey { get; set; }
        public Keys HotkeyModifiers { get; set; }
        public bool OverlayEnabled { get; set; }
        public int OverlayX { get; set; }
        public int OverlayY { get; set; }
        public int OverlaySize { get { return overlaySize; } set { overlaySize = ClampOverlaySize(value); } }
        public int OverlayOpacity { get { return overlayOpacity; } set { overlayOpacity = Math.Max(25, Math.Min(100, value)); } }
        public bool LockOverlay { get; set; }
        public bool SoundsEnabled { get; set; }
        public bool SoundOnExternalChanges { get; set; }
        public bool CheckUpdatesOnStartup { get; set; }
        public string TrayOnIcon { get; set; }
        public string TrayOffIcon { get; set; }
        public string OverlayOnImage { get; set; }
        public string OverlayOffImage { get; set; }
        public string MuteSound { get; set; }
        public string UnmuteSound { get; set; }
        public SoundSource MuteSource { get { return ResolveSource(muteSource, MuteSound); } set { muteSource = value; } }
        public SoundSource UnmuteSource { get { return ResolveSource(unmuteSource, UnmuteSound); } set { unmuteSource = value; } }

        private static SoundSource ResolveSource(SoundSource? source, string path)
        {
            // Old configs only stored a path; keep their custom override without guessing a mode.
            return source ?? (String.IsNullOrWhiteSpace(path) ? SoundSource.Style : SoundSource.Custom);
        }

        public AppConfig()
        {
            AppLanguage = "en";
            MicrophoneDeviceId = "";
            HotkeyEnabled = true;
            HotkeyKey = Keys.F13;
            HotkeyModifiers = Keys.None;
            OverlayEnabled = true;
            OverlayX = int.MinValue;
            OverlayY = int.MinValue;
            SoundsEnabled = true;
            CheckUpdatesOnStartup = true;
            TrayOnIcon = TrayOffIcon = OverlayOnImage = OverlayOffImage = MuteSound = UnmuteSound = "";
        }

        public AppConfig Clone()
        {
            return (AppConfig)MemberwiseClone();
        }

        public static int ClampOverlaySize(int size)
        {
            return Math.Max(40, Math.Min(256, size));
        }

        public static AppConfig Load(string path)
        {
            var config = new AppConfig();
            if (!File.Exists(path))
                return config;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (FileNotFoundException) { return config; }
            catch (DirectoryNotFoundException) { return config; }

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed[0] == ';')
                    continue;
                int separator = trimmed.IndexOf('=');
                if (separator <= 0)
                    continue;
                string key = trimmed.Substring(0, separator).Trim().ToUpperInvariant();
                string value = trimmed.Substring(separator + 1).Trim();
                bool flag;
                int number;
                Keys keys;
                SoundSource soundSource;
                switch (key)
                {
                    case "APP_LANGUAGE": config.AppLanguage = UiText.NormalizeLanguage(value); break;
                    case "ICON_STYLE": if (TryParseNumber(value, out number)) config.IconStyle = number; break;
                    case "SOUND_STYLE": if (TryParseNumber(value, out number)) config.SoundStyle = number; break;
                    case "MUTE_SOURCE": if (TryParseSoundSource(value, out soundSource)) config.MuteSource = soundSource; break;
                    case "UNMUTE_SOURCE": if (TryParseSoundSource(value, out soundSource)) config.UnmuteSource = soundSource; break;
                    case "MICROPHONE_DEVICE_ID": if (IsSingleLine(value)) config.MicrophoneDeviceId = value; break;
                    case "HOTKEY_ENABLED": if (TryParseBoolean(value, out flag)) config.HotkeyEnabled = flag; break;
                    case "HOTKEY_KEY": if (Enum.TryParse<Keys>(value, true, out keys) && IsValidHotkeyKey(keys)) config.HotkeyKey = keys; break;
                    case "HOTKEY_MODIFIERS": if (Enum.TryParse<Keys>(value, true, out keys) && IsValidModifiers(keys)) config.HotkeyModifiers = keys; break;
                    case "OVERLAY_ENABLED": if (TryParseBoolean(value, out flag)) config.OverlayEnabled = flag; break;
                    case "OVERLAY_X": if (TryParseNumber(value, out number)) config.OverlayX = number; break;
                    case "OVERLAY_Y": if (TryParseNumber(value, out number)) config.OverlayY = number; break;
                    case "OVERLAY_SIZE": if (TryParseNumber(value, out number)) config.OverlaySize = number; break;
                    case "OVERLAY_OPACITY": if (TryParseNumber(value, out number)) config.OverlayOpacity = number; break;
                    case "LOCK_OVERLAY": if (TryParseBoolean(value, out flag)) config.LockOverlay = flag; break;
                    case "SOUNDS_ENABLED": if (TryParseBoolean(value, out flag)) config.SoundsEnabled = flag; break;
                    case "SOUND_ON_EXTERNAL_CHANGES": if (TryParseBoolean(value, out flag)) config.SoundOnExternalChanges = flag; break;
                    case "CHECK_UPDATES_ON_STARTUP": if (TryParseBoolean(value, out flag)) config.CheckUpdatesOnStartup = flag; break;
                    case "TRAY_ON_ICON": if (IsSingleLine(value)) config.TrayOnIcon = value; break;
                    case "TRAY_OFF_ICON": if (IsSingleLine(value)) config.TrayOffIcon = value; break;
                    case "OVERLAY_ON_IMAGE": if (IsSingleLine(value)) config.OverlayOnImage = value; break;
                    case "OVERLAY_OFF_IMAGE": if (IsSingleLine(value)) config.OverlayOffImage = value; break;
                    case "MUTE_SOUND": if (IsSingleLine(value)) config.MuteSound = value; break;
                    case "UNMUTE_SOUND": if (IsSingleLine(value)) config.UnmuteSound = value; break;
                }
            }
            if (config.HotkeyEnabled && config.HotkeyKey == Keys.None)
                config.HotkeyKey = Keys.F13;
            return config;
        }

        public void Save(string path)
        {
            if (!IsValidHotkeyKey(HotkeyKey) || (HotkeyEnabled && HotkeyKey == Keys.None) || !IsValidModifiers(HotkeyModifiers))
                throw new ArgumentException("The hotkey must contain a keyboard key and only Ctrl, Alt, or Shift modifiers.");
            string language = (AppLanguage ?? "").Trim().ToLowerInvariant();
            if (!UiText.IsSupportedLanguage(language))
                throw new ArgumentException("Unsupported application language.");

            var text = new StringBuilder();
            Append(text, "APP_LANGUAGE", language);
            Append(text, "ICON_STYLE", IconStyle);
            Append(text, "SOUND_STYLE", SoundStyle);
            Append(text, "MUTE_SOURCE", MuteSource.ToString());
            Append(text, "UNMUTE_SOURCE", UnmuteSource.ToString());
            Append(text, "MICROPHONE_DEVICE_ID", MicrophoneDeviceId);
            Append(text, "HOTKEY_ENABLED", HotkeyEnabled);
            Append(text, "HOTKEY_KEY", HotkeyKey.ToString());
            Append(text, "HOTKEY_MODIFIERS", HotkeyModifiers.ToString());
            Append(text, "OVERLAY_ENABLED", OverlayEnabled);
            Append(text, "OVERLAY_X", OverlayX);
            Append(text, "OVERLAY_Y", OverlayY);
            Append(text, "OVERLAY_SIZE", OverlaySize);
            Append(text, "OVERLAY_OPACITY", OverlayOpacity);
            Append(text, "LOCK_OVERLAY", LockOverlay);
            Append(text, "SOUNDS_ENABLED", SoundsEnabled);
            Append(text, "SOUND_ON_EXTERNAL_CHANGES", SoundOnExternalChanges);
            Append(text, "CHECK_UPDATES_ON_STARTUP", CheckUpdatesOnStartup);
            Append(text, "TRAY_ON_ICON", TrayOnIcon);
            Append(text, "TRAY_OFF_ICON", TrayOffIcon);
            Append(text, "OVERLAY_ON_IMAGE", OverlayOnImage);
            Append(text, "OVERLAY_OFF_IMAGE", OverlayOffImage);
            Append(text, "MUTE_SOUND", MuteSound);
            Append(text, "UNMUTE_SOUND", UnmuteSound);

            if (string.IsNullOrEmpty(path) || !IsSingleLine(path))
                throw new ArgumentException("A valid configuration file path is required.", "path");
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);
            string temporary = Path.Combine(directory, Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temporary, text.ToString(), new UTF8Encoding(false));
                if (File.Exists(fullPath))
                    File.Replace(temporary, fullPath, fullPath + ".bak");
                else
                    File.Move(temporary, fullPath);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Append(StringBuilder text, string key, string value)
        {
            if (!IsSingleLine(value))
                throw new ArgumentException("Configuration value " + key + " cannot contain line breaks or NUL characters.");
            text.Append(key).Append('=').Append(value ?? "").AppendLine();
        }

        private static void Append(StringBuilder text, string key, bool value)
        {
            Append(text, key, value ? "true" : "false");
        }

        private static void Append(StringBuilder text, string key, int value)
        {
            Append(text, key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool IsSingleLine(string value)
        {
            return value == null || (value.IndexOf('\r') < 0 && value.IndexOf('\n') < 0 && value.IndexOf('\0') < 0);
        }

        private static bool TryParseBoolean(string value, out bool result)
        {
            if (value == "1") { result = true; return true; }
            if (value == "0") { result = false; return true; }
            return bool.TryParse(value, out result);
        }

        private static bool TryParseNumber(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseSoundSource(string value, out SoundSource result)
        { return Enum.TryParse<SoundSource>(value, true, out result) && Enum.IsDefined(typeof(SoundSource), result); }

        private static bool IsValidModifiers(Keys value)
        {
            return (value & ~(Keys.Control | Keys.Alt | Keys.Shift)) == Keys.None;
        }

        private static bool IsValidHotkeyKey(Keys key)
        {
            if (key == Keys.None)
                return true;
            int value = (int)key;
            return value >= (int)Keys.Back && value <= 254 && Enum.IsDefined(typeof(Keys), key)
                && key != Keys.ShiftKey && key != Keys.ControlKey && key != Keys.Menu
                && key != Keys.LShiftKey && key != Keys.RShiftKey
                && key != Keys.LControlKey && key != Keys.RControlKey
                && key != Keys.LMenu && key != Keys.RMenu && key != Keys.LWin && key != Keys.RWin;
        }
    }
}
