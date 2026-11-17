using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("MicMute")]
[assembly: AssemblyDescription("Minimal customizable microphone controller")]
[assembly: AssemblyVersion("3.1.0.0")]
[assembly: AssemblyFileVersion("3.1.0.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]

namespace MicMute
{
    internal static class Program
    {
        internal const string Version = "3.1.0";
        internal static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MicMuteConfig.txt");
        [STAThread]
        private static void Main(string[] args)
        {
            bool created;
            using (var mutex = new Mutex(true, "Local\\MicMute.OpenSource.v3", out created))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                string language = "en";
                try
                {
                    bool firstRun = !File.Exists(ConfigPath);
                    var config = AppConfig.Load(ConfigPath);
                    language = config.AppLanguage;
                    if (!created) { MessageBox.Show(UiText.Get("MicMute уже запущен. Откройте настройки через значок в трее или на экране.", language), "MicMute"); return; }
                    using (var app = new MicMuteContext(config, firstRun)) Application.Run(app);
                }
                catch (Exception ex) { MessageBox.Show(UiText.Get("Не удалось запустить MicMute.\n", language) + ex.Message, "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { if (created) mutex.ReleaseMutex(); }
            }
        }
    }

    internal sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr window, int id);
        public event EventHandler Pressed;
        private bool registered;

        public HotkeyWindow() { CreateHandle(new CreateParams { Caption = "MicMute hotkey", Parent = new IntPtr(-3) }); }

        public bool Apply(AppConfig config)
        {
            if (registered) { UnregisterHotKey(Handle, 1); registered = false; }
            if (!config.HotkeyEnabled) return true;
            if (config.HotkeyKey == Keys.None) return false;
            uint modifiers = 0x4000; // MOD_NOREPEAT: holding the key must not repeatedly toggle.
            if ((config.HotkeyModifiers & Keys.Control) != 0) modifiers |= 2;
            if ((config.HotkeyModifiers & Keys.Alt) != 0) modifiers |= 1;
            if ((config.HotkeyModifiers & Keys.Shift) != 0) modifiers |= 4;
            registered = RegisterHotKey(Handle, 1, modifiers, (uint)config.HotkeyKey);
            return registered;
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0312 && message.WParam.ToInt32() == 1 && Pressed != null) Pressed(this, EventArgs.Empty);
            base.WndProc(ref message);
        }
        public void Dispose() { if (registered) UnregisterHotKey(Handle, 1); DestroyHandle(); }
    }

    internal sealed class MicMuteContext : ApplicationContext
    {
        private AppConfig config;
        private VisualAssets assets;
        private readonly NotifyIcon tray;
        private readonly OverlayForm overlay;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem toggleItem, settingsItem, exitItem;
        private readonly HotkeyWindow hotkey;
        private readonly System.Windows.Forms.Timer timer;
        private bool? state;
        private bool statusInitialized, settingsOpen, disposed, saveErrorShown;

        public MicMuteContext(AppConfig initialConfig, bool firstRun)
        {
            config = initialConfig;
            assets = new VisualAssets(config);
            menu = new ContextMenuStrip();
            toggleItem = new ToolStripMenuItem(T("Переключить микрофон"), null, (s, e) => Toggle());
            menu.Items.Add(toggleItem);
            settingsItem = new ToolStripMenuItem(T("Настройки…"), null, (s, e) => ShowSettings());
            menu.Items.Add(settingsItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("MicMute " + Program.Version) { Enabled = false });
            exitItem = new ToolStripMenuItem(T("Выход"), null, (s, e) => ExitThread());
            menu.Items.Add(exitItem);
            tray = new NotifyIcon { ContextMenuStrip = menu, Icon = assets.UnknownIcon, Text = "MicMute", Visible = true };
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) Toggle(); };
            overlay = new OverlayForm(menu);
            overlay.ToggleRequested += (s, e) => Toggle();
            overlay.PlacementChanged += (s, e) => SavePlacement();
            overlay.ApplyConfig(config, assets, false);
            hotkey = new HotkeyWindow();
            hotkey.Pressed += (s, e) => { if (!settingsOpen) Toggle(); };
            if (!hotkey.Apply(config)) Balloon("Горячая клавиша недоступна. Выберите другую в настройках.");
            RefreshStatus(false);
            timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, e) =>
            {
                if (!settingsOpen) overlay.RaiseAboveWindows();
                RefreshStatus(true);
            };
            timer.Start();
            if (firstRun)
            {
                EventHandler onIdle = null;
                onIdle = (s, e) => { Application.Idle -= onIdle; if (!disposed) ShowSettings(); };
                Application.Idle += onIdle;
            }
        }

        private void RefreshStatus(bool external)
        {
            bool? next = AudioController.GetMuteState(config.MicrophoneDeviceId);
            if (statusInitialized && next == state) return;
            bool canPlay = external && statusInitialized && state.HasValue && next.HasValue && next != state;
            state = next; statusInitialized = true;
            tray.Icon = !state.HasValue ? assets.UnknownIcon : state.Value ? assets.MutedIcon : assets.ActiveIcon;
            tray.Text = T(!state.HasValue ? "MicMute — микрофон недоступен" : state.Value ? "MicMute — микрофон выключен" : "MicMute — микрофон включён");
            toggleItem.Enabled = state.HasValue;
            toggleItem.Text = T(state == true ? "Включить микрофон" : "Выключить микрофон");
            overlay.ApplyStatus(state);
            if (canPlay && config.SoundsEnabled && config.SoundOnExternalChanges) assets.Play(state.Value);
        }

        private void Toggle()
        {
            bool? actual = AudioController.GetMuteState(config.MicrophoneDeviceId);
            if (!actual.HasValue)
            {
                RefreshStatus(false);
                Balloon("Микрофон недоступен. Подключите его или выберите устройство в настройках.");
                return;
            }
            bool requested = !actual.Value;
            bool success = AudioController.SetMuteState(config.MicrophoneDeviceId, requested);
            RefreshStatus(false);
            if (!success || state != requested) { Balloon("Windows не подтвердила переключение микрофона. Проверьте выбранное устройство."); return; }
            if (config.SoundsEnabled) assets.Play(requested);
        }

        private void ShowSettings()
        {
            if (settingsOpen) return;
            settingsOpen = true;
            // Suspend this app's hotkey while capturing a replacement in the dialog.
            hotkey.Apply(new AppConfig { HotkeyEnabled = false });
            try
            {
                using (var dialog = new SettingsForm(config))
                {
                    while (dialog.ShowDialog() == DialogResult.OK)
                    {
                        AppConfig candidate = dialog.Result;
                        if (!hotkey.Apply(candidate))
                        {
                            MessageBox.Show(UiText.Get("Эта горячая клавиша уже занята или не поддерживается. Выберите другую либо отключите её.", candidate.AppLanguage), "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            dialog.DialogResult = DialogResult.None;
                            continue;
                        }
                        if (dialog.ResetOverlayPosition) { candidate.OverlayX = int.MinValue; candidate.OverlayY = int.MinValue; }
                        VisualAssets replacement = null;
                        try
                        {
                            replacement = new VisualAssets(candidate);
                            candidate.Save(Program.ConfigPath);
                        }
                        catch (Exception ex)
                        {
                            if (replacement != null) replacement.Dispose();
                            hotkey.Apply(new AppConfig { HotkeyEnabled = false });
                            MessageBox.Show(UiText.Get("Не удалось сохранить настройки.\n", candidate.AppLanguage) + ex.Message, "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            dialog.DialogResult = DialogResult.None;
                            continue;
                        }
                        VisualAssets previous = assets;
                        config = candidate; assets = replacement;
                        settingsItem.Text = T("Настройки…");
                        exitItem.Text = T("Выход");
                        overlay.ApplyConfig(config, assets, dialog.ResetOverlayPosition);
                        statusInitialized = false;
                        RefreshStatus(false);
                        previous.Dispose();
                        saveErrorShown = false;
                        break;
                    }
                }
            }
            finally { settingsOpen = false; hotkey.Apply(config); overlay.RaiseAboveWindows(); }
        }

        private void SavePlacement()
        {
            try { config.Save(Program.ConfigPath); saveErrorShown = false; }
            catch (Exception) { if (!saveErrorShown) { Balloon("Не удалось сохранить позицию. Проверьте доступ к папке программы."); saveErrorShown = true; } }
        }

        private string T(string text) { return UiText.Get(text, config.AppLanguage); }
        private void Balloon(string text) { tray.ShowBalloonTip(4000, "MicMute", T(text), ToolTipIcon.Warning); }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                timer.Stop(); timer.Dispose();
                hotkey.Dispose(); overlay.Dispose();
                tray.Visible = false; tray.Dispose(); menu.Dispose(); assets.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
