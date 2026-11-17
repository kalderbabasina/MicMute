using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class SettingsForm : Form
    {
        private readonly Font dialogFont = new Font("Segoe UI", 9F);
        private readonly ToolTip toolTip = new ToolTip();
        private readonly TabControl tabs = new TabControl();
        private readonly Dictionary<Control, string> originalText = new Dictionary<Control, string>();
        private readonly Dictionary<Control, Func<string>> accessibleText = new Dictionary<Control, Func<string>>();
        private static readonly string[] IconStyleNames = { "Классический", "Контурный", "Пиксельный" };
        private static readonly string[] SoundStyleNames = { "Windows", "Мягкий", "Аркадный" };
        private readonly ComboBox languageSelector = new ComboBox();
        private readonly ComboBox iconStyle = new ComboBox();
        private readonly ComboBox soundStyle = new ComboBox();
        private readonly PictureBox activePreview = new PictureBox();
        private readonly PictureBox mutedPreview = new PictureBox();
        private readonly List<AudioDeviceInfo> availableDevices = new List<AudioDeviceInfo>();
        private readonly ComboBox microphone = new ComboBox();
        private readonly Label microphoneError = new Label();
        private readonly CheckBox hotkeyEnabled = new CheckBox();
        private readonly TextBox hotkey = new TextBox();
        private readonly CheckBox overlayEnabled = new CheckBox();
        private readonly NumericUpDown overlaySize = new NumericUpDown();
        private readonly TrackBar opacitySlider = new TrackBar();
        private readonly NumericUpDown opacityNumber = new NumericUpDown();
        private readonly CheckBox lockOverlay = new CheckBox();
        private readonly CheckBox soundsEnabled = new CheckBox();
        private readonly CheckBox externalSounds = new CheckBox();
        private TextBox trayOn;
        private TextBox trayOff;
        private TextBox overlayOn;
        private TextBox overlayOff;
        private TextBox muteSound;
        private TextBox unmuteSound;
        private Keys capturedKey;
        private Keys capturedModifiers;
        private bool resetRequested;
        private bool applyingLanguage;
        private string language = "en";
        private string deviceEnumerationError;
        private VisualAssets previewVisuals;

        public AppConfig Result { get; private set; }
        public bool ResetOverlayPosition { get; private set; }

        public SettingsForm(AppConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            Result = config.Clone();
            language = UiText.NormalizeLanguage(config.AppLanguage);
            BindText(this, "MicMute — настройки");
            Font = dialogFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            ClientSize = new Size(620, 580);
            MinimumSize = new Size(620, 580);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = SystemColors.Control;

            TableLayoutPanel root = Stack();
            root.Dock = DockStyle.Fill;
            root.AutoSize = false;
            root.Padding = new Padding(12);
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);
            tabs.Dock = DockStyle.Fill;
            root.Controls.Add(tabs, 0, 0);

            BuildGeneralTab();
            BuildAppearanceTab();
            BuildSoundTab();

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.Padding = new Padding(0, 8, 0, 0);
            Button cancel = MakeButton("Отмена");
            cancel.DialogResult = DialogResult.Cancel;
            Button ok = MakeButton("ОК");
            ok.Click += SaveSettings;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            root.Controls.Add(buttons, 0, 1);
            CancelButton = cancel;
            AcceptButton = ok;
            RefreshMicrophones(Result.MicrophoneDeviceId);
            ApplyLanguage(language);
            UpdateIconPreviews();
        }

        private void BuildGeneralTab()
        {
            TableLayoutPanel page = AddPage("Основное");
            TableLayoutPanel languages = Grid(2);
            languages.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            languages.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            languages.Margin = new Padding(0, 0, 0, 8);
            languages.Controls.Add(FieldLabel("Язык"), 0, 0);
            languageSelector.Name = "Language";
            languageSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            languageSelector.Dock = DockStyle.Fill;
            languageSelector.Items.AddRange(new object[] { "English", "Русский", "Español" });
            languageSelector.SelectedIndex = language == "ru" ? 1 : language == "es" ? 2 : 0;
            BindAccessible(languageSelector, "Язык");
            languageSelector.SelectedIndexChanged += delegate
            {
                if (!applyingLanguage) ApplyLanguage(languageSelector.SelectedIndex == 1 ? "ru" : languageSelector.SelectedIndex == 2 ? "es" : "en");
            };
            languages.Controls.Add(languageSelector, 1, 0);
            page.Controls.Add(languages);
            TableLayoutPanel devices = Grid(2);
            devices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            devices.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            microphone.DropDownStyle = ComboBoxStyle.DropDownList;
            microphone.Dock = DockStyle.Fill;
            microphone.DropDownWidth = 500;
            BindAccessible(microphone, "Микрофон");
            Button refresh = MakeButton("Обновить");
            refresh.Click += delegate { RefreshMicrophones(SelectedDeviceId()); };
            devices.Controls.Add(microphone, 0, 0);
            devices.Controls.Add(refresh, 1, 0);
            microphoneError.AutoSize = true;
            microphoneError.Dock = DockStyle.Fill;
            microphoneError.MaximumSize = new Size(520, 0);
            microphoneError.ForeColor = Color.Firebrick;
            devices.Controls.Add(microphoneError, 0, 1);
            devices.SetColumnSpan(microphoneError, 2);
            AddGroup(page, "Микрофон", devices);

            TableLayoutPanel hotkeys = Stack();
            BindText(hotkeyEnabled, "Включить глобальную горячую клавишу");
            hotkeyEnabled.AutoSize = true;
            hotkeyEnabled.Checked = Result.HotkeyEnabled;
            hotkeys.Controls.Add(hotkeyEnabled);
            hotkey.ReadOnly = true;
            hotkey.Dock = DockStyle.Fill;
            BindAccessible(hotkey, "Горячая клавиша: нажмите сочетание");
            capturedKey = Result.HotkeyKey & Keys.KeyCode;
            capturedModifiers = Result.HotkeyModifiers & (Keys.Control | Keys.Alt | Keys.Shift);
            if (!IsHotkeyKey(capturedKey)) capturedKey = Keys.None;
            UpdateHotkeyText();
            hotkey.KeyDown += CaptureKeyDown;
            hotkey.Enabled = hotkeyEnabled.Checked;
            hotkeyEnabled.CheckedChanged += delegate { hotkey.Enabled = hotkeyEnabled.Checked; };
            hotkeys.Controls.Add(hotkey);
            hotkeys.Controls.Add(Hint("Нажмите клавишу или сочетание с Ctrl, Alt, Shift. Tab — переход, Esc — отмена, Delete / Backspace — очистить."));
            AddGroup(page, "Горячая клавиша", hotkeys);

            TableLayoutPanel overlay = Grid(2);
            overlay.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            overlay.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            BindText(overlayEnabled, "Показывать плавающий индикатор");
            overlayEnabled.AutoSize = true;
            overlayEnabled.Checked = Result.OverlayEnabled;
            overlay.Controls.Add(overlayEnabled, 0, 0);
            overlay.SetColumnSpan(overlayEnabled, 2);
            overlay.Controls.Add(FieldLabel("Размер, пиксели"), 0, 1);
            overlaySize.Minimum = 40;
            overlaySize.Maximum = 256;
            overlaySize.Value = Math.Max(40, Math.Min(256, Result.OverlaySize));
            overlaySize.Width = 86;
            BindAccessible(overlaySize, "Размер индикатора в пикселях");
            overlay.Controls.Add(overlaySize, 1, 1);
            overlay.Controls.Add(FieldLabel("Непрозрачность, %"), 0, 2);
            TableLayoutPanel opacity = Grid(2);
            opacity.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            opacity.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            opacitySlider.Minimum = 25;
            opacitySlider.Maximum = 100;
            opacitySlider.Value = Math.Max(25, Math.Min(100, Result.OverlayOpacity));
            opacitySlider.TickFrequency = 25;
            opacitySlider.AutoSize = false;
            opacitySlider.Height = 30;
            opacitySlider.Dock = DockStyle.Fill;
            BindAccessible(opacitySlider, "Непрозрачность индикатора");
            opacityNumber.Minimum = 25;
            opacityNumber.Maximum = 100;
            opacityNumber.Value = opacitySlider.Value;
            opacityNumber.Width = 66;
            BindAccessible(opacityNumber, "Непрозрачность индикатора в процентах");
            opacitySlider.ValueChanged += delegate { opacityNumber.Value = opacitySlider.Value; };
            opacityNumber.ValueChanged += delegate { opacitySlider.Value = (int)opacityNumber.Value; };
            opacity.Controls.Add(opacitySlider, 0, 0);
            opacity.Controls.Add(opacityNumber, 1, 0);
            overlay.Controls.Add(opacity, 1, 2);
            BindText(lockOverlay, "Заблокировать положение и размер");
            lockOverlay.AutoSize = true;
            lockOverlay.Checked = Result.LockOverlay;
            overlay.Controls.Add(lockOverlay, 0, 3);
            overlay.SetColumnSpan(lockOverlay, 2);
            Button reset = MakeButton("Сбросить позицию");
            reset.Anchor = AnchorStyles.Left;
            overlay.Controls.Add(reset, 0, 4);
            overlay.SetColumnSpan(reset, 2);
            Label resetStatus = Hint("Позиция будет сброшена после нажатия ОК.");
            resetStatus.Visible = false;
            reset.Click += delegate { resetRequested = true; resetStatus.Visible = true; };
            overlay.Controls.Add(resetStatus, 0, 5);
            overlay.SetColumnSpan(resetStatus, 2);
            Label dragHint = Hint("Перетаскивание — перемещение; Shift + перетаскивание — изменение размера. Блокировка отключает оба действия.");
            overlay.Controls.Add(dragHint, 0, 6);
            overlay.SetColumnSpan(dragHint, 2);
            EventHandler updateOverlayEnabled = delegate
            {
                bool enabled = overlayEnabled.Checked;
                overlaySize.Enabled = enabled;
                opacity.Enabled = enabled;
                lockOverlay.Enabled = enabled;
                reset.Enabled = enabled;
            };
            overlayEnabled.CheckedChanged += updateOverlayEnabled;
            updateOverlayEnabled(null, EventArgs.Empty);
            AddGroup(page, "Индикатор", overlay);
        }

        private void BuildAppearanceTab()
        {
            TableLayoutPanel page = AddPage("Внешний вид");
            TableLayoutPanel styles = Grid(2);
            styles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            styles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            SetUpStyleSelector(iconStyle, "IconStyle", "Стиль значков", IconStyleNames, Result.IconStyle);
            styles.Controls.Add(iconStyle, 0, 0);
            styles.SetColumnSpan(iconStyle, 2);
            styles.Controls.Add(IconSample(activePreview, "Включён"), 0, 1);
            styles.Controls.Add(IconSample(mutedPreview, "Выключен"), 1, 1);
            iconStyle.SelectedIndexChanged += delegate { if (!applyingLanguage) UpdateIconPreviews(); };
            AddGroup(page, "Стиль значков", styles);
            page.Controls.Add(Hint("Пустое поле — встроенное оформление. Файлы не копируются: храните их по выбранному пути."));
            TableLayoutPanel tray = FileGrid();
            trayOn = AddFileRow(tray, "Включён", Result.TrayOnIcon, FileKind.Icon);
            trayOff = AddFileRow(tray, "Выключен", Result.TrayOffIcon, FileKind.Icon);
            AddGroup(page, "Значок в области уведомлений (ICO)", tray);
            TableLayoutPanel images = FileGrid();
            overlayOn = AddFileRow(images, "Включён", Result.OverlayOnImage, FileKind.Image);
            overlayOff = AddFileRow(images, "Выключен", Result.OverlayOffImage, FileKind.Image);
            AddGroup(page, "Плавающий индикатор (PNG, BMP, JPG, ICO)", images);
            page.Controls.Add(Hint("Ограничения: до 16 МБ на файл и до 4096 × 4096 пикселей. Для прозрачного фона используйте PNG или ICO."));
        }

        private void BuildSoundTab()
        {
            TableLayoutPanel page = AddPage("Звуки");
            BindText(soundsEnabled, "Воспроизводить звуки переключения");
            soundsEnabled.AutoSize = true;
            soundsEnabled.Checked = Result.SoundsEnabled;
            page.Controls.Add(soundsEnabled);
            BindText(externalSounds, "Также при изменениях из других приложений");
            externalSounds.AutoSize = true;
            externalSounds.Checked = Result.SoundOnExternalChanges;
            page.Controls.Add(externalSounds);
            TableLayoutPanel styles = Grid(3);
            styles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            styles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            styles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            SetUpStyleSelector(soundStyle, "SoundStyle", "Стиль звуков", SoundStyleNames, Result.SoundStyle);
            styles.Controls.Add(soundStyle, 0, 0);
            Button previewMute = MakeButton("Прослушать выключение");
            previewMute.Click += delegate { PreviewSound(true); };
            styles.Controls.Add(previewMute, 1, 0);
            Button previewUnmute = MakeButton("Прослушать включение");
            previewUnmute.Click += delegate { PreviewSound(false); };
            styles.Controls.Add(previewUnmute, 2, 0);
            AddGroup(page, "Стиль звуков", styles);

            TableLayoutPanel sounds = FileGrid();
            muteSound = AddFileRow(sounds, "Выключение", Result.MuteSound, FileKind.Sound);
            unmuteSound = AddFileRow(sounds, "Включение", Result.UnmuteSound, FileKind.Sound);
            soundStyle.SelectedIndexChanged += delegate { if (!applyingLanguage) ResetSoundPreview(); };
            muteSound.TextChanged += delegate { ResetSoundPreview(); };
            unmuteSound.TextChanged += delegate { ResetSoundPreview(); };
            GroupBox soundGroup = AddGroup(page, "Звуковые файлы (WAV, MP3)", sounds);
            page.Controls.Add(Hint("Пустое поле — звук выбранного стиля. WAV и MP3: до 16 МБ на файл. Храните файлы по выбранному пути."));
            EventHandler updateSoundsEnabled = delegate
            {
                externalSounds.Enabled = soundsEnabled.Checked;
                soundGroup.Enabled = soundsEnabled.Checked;
            };
            soundsEnabled.CheckedChanged += updateSoundsEnabled;
            updateSoundsEnabled(null, EventArgs.Empty);
        }

        private void SetUpStyleSelector(ComboBox combo, string name, string label, string[] styleNames, int selected)
        {
            combo.Name = name;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Dock = DockStyle.Fill;
            BindAccessible(combo, label);
            SetStyleNames(combo, styleNames, selected);
        }

        private void SetStyleNames(ComboBox combo, string[] styleNames, int selected)
        {
            combo.BeginUpdate();
            try
            {
                combo.Items.Clear();
                foreach (string name in styleNames) combo.Items.Add(T(name));
                combo.SelectedIndex = selected;
            }
            finally { combo.EndUpdate(); }
        }

        private Control IconSample(PictureBox picture, string label)
        {
            FlowLayoutPanel sample = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
            picture.Size = new Size(36, 36);
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            BindAccessible(picture, label);
            sample.Controls.Add(picture);
            sample.Controls.Add(FieldLabel(label));
            return sample;
        }

        private void UpdateIconPreviews()
        {
            if (iconStyle.SelectedIndex < 0) return;
            SetIconPreview(activePreview, false);
            SetIconPreview(mutedPreview, true);
        }

        private void SetIconPreview(PictureBox picture, bool muted)
        {
            using (Icon icon = VisualAssets.CreateStatusIcon(muted, iconStyle.SelectedIndex))
                SetPreviewImage(picture, icon.ToBitmap());
        }

        private static void SetPreviewImage(PictureBox picture, Image next)
        {
            Image previous = picture.Image;
            picture.Image = next;
            if (previous != null) previous.Dispose();
        }

        private void PreviewSound(bool muted)
        {
            if (previewVisuals == null)
                previewVisuals = new VisualAssets(new AppConfig
                {
                    SoundStyle = soundStyle.SelectedIndex,
                    MuteSound = muteSound.Text,
                    UnmuteSound = unmuteSound.Text
                });
            previewVisuals.Play(muted);
        }

        private void ResetSoundPreview()
        {
            if (previewVisuals == null) return;
            previewVisuals.Dispose();
            previewVisuals = null;
        }

        private void RefreshMicrophones(string preferredId)
        {
            availableDevices.Clear();
            deviceEnumerationError = null;
            try { availableDevices.AddRange(AudioController.GetAvailableMicrophones()); }
            catch (Exception ex) { deviceEnumerationError = ex.Message; }
            UpdateMicrophoneChoices(preferredId);
        }

        private void UpdateMicrophoneChoices(string preferredId)
        {
            preferredId = preferredId ?? String.Empty;
            microphone.BeginUpdate();
            try
            {
                microphone.Items.Clear();
                microphone.Items.Add(new DeviceChoice(String.Empty, T("По умолчанию (устройство связи)")));
                foreach (AudioDeviceInfo device in availableDevices)
                {
                    if (String.IsNullOrEmpty(device.Id)) continue;
                    microphone.Items.Add(new DeviceChoice(device.Id, String.IsNullOrEmpty(device.Name) ? device.Id : device.Name));
                }
                microphoneError.Text = deviceEnumerationError == null ? String.Empty :
                    String.Format(T("Не удалось получить список микрофонов: {0}"), deviceEnumerationError);
                microphoneError.Visible = deviceEnumerationError != null;
                int selected = -1;
                for (int i = 0; i < microphone.Items.Count; i++)
                {
                    DeviceChoice item = (DeviceChoice)microphone.Items[i];
                    if (String.Equals(item.Id, preferredId, StringComparison.Ordinal))
                    {
                        selected = i;
                        break;
                    }
                }
                if (selected < 0)
                    selected = microphone.Items.Add(new DeviceChoice(preferredId, String.Format(T("Недоступен: {0}"), preferredId)));
                microphone.SelectedIndex = selected;
            }
            finally { microphone.EndUpdate(); }
        }

        private string SelectedDeviceId()
        {
            DeviceChoice selected = microphone.SelectedItem as DeviceChoice;
            return selected == null ? String.Empty : selected.Id;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (hotkey.Focused && (keyData & Keys.KeyCode) != Keys.Tab &&
                (keyData & Keys.KeyCode) != Keys.Escape && keyData != (Keys.Alt | Keys.F4))
            {
                CaptureHotkey(keyData);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CaptureKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape) return;
            CaptureHotkey(e.KeyData);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void CaptureHotkey(Keys keyData)
        {
            if ((GetKeyState((int)Keys.LWin) & 0x8000) != 0 ||
                (GetKeyState((int)Keys.RWin) & 0x8000) != 0) return;
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Delete || key == Keys.Back)
            {
                capturedKey = Keys.None;
                capturedModifiers = Keys.None;
            }
            else
            {
                if (!IsHotkeyKey(key)) return;
                capturedKey = key;
                capturedModifiers = keyData & (Keys.Control | Keys.Alt | Keys.Shift);
            }
            UpdateHotkeyText();
        }

        private static bool IsHotkeyKey(Keys key)
        {
            return (int)key >= (int)Keys.Back && (int)key <= 254 && Enum.IsDefined(typeof(Keys), key) &&
                key != Keys.ControlKey && key != Keys.LControlKey &&
                key != Keys.RControlKey && key != Keys.ShiftKey && key != Keys.LShiftKey &&
                key != Keys.RShiftKey && key != Keys.Menu && key != Keys.LMenu && key != Keys.RMenu &&
                key != Keys.LWin && key != Keys.RWin && key != Keys.Tab && key != Keys.Escape &&
                key != Keys.Delete && key != Keys.Back;
        }

        private void UpdateHotkeyText()
        {
            if (capturedKey == Keys.None) { hotkey.Text = T("Не задана"); return; }
            string value = String.Empty;
            if ((capturedModifiers & Keys.Control) != 0) value += "Ctrl + ";
            if ((capturedModifiers & Keys.Alt) != 0) value += "Alt + ";
            if ((capturedModifiers & Keys.Shift) != 0) value += "Shift + ";
            if (capturedKey >= Keys.D0 && capturedKey <= Keys.D9)
                value += ((int)capturedKey - (int)Keys.D0).ToString();
            else value += new KeysConverter().ConvertToString(capturedKey);
            hotkey.Text = value;
        }

        private void SaveSettings(object sender, EventArgs e)
        {
            if (hotkeyEnabled.Checked && !IsHotkeyKey(capturedKey))
            {
                tabs.SelectedIndex = 0;
                MessageBox.Show(this, T("Задайте горячую клавишу или отключите её использование."),
                    T("Горячая клавиша"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                hotkey.Focus();
                return;
            }
            Result.AppLanguage = language;
            Result.IconStyle = iconStyle.SelectedIndex;
            Result.SoundStyle = soundStyle.SelectedIndex;
            Result.MicrophoneDeviceId = SelectedDeviceId();
            Result.HotkeyEnabled = hotkeyEnabled.Checked;
            Result.HotkeyKey = capturedKey;
            Result.HotkeyModifiers = capturedModifiers;
            Result.OverlayEnabled = overlayEnabled.Checked;
            Result.OverlaySize = (int)overlaySize.Value;
            Result.OverlayOpacity = (int)opacityNumber.Value;
            Result.LockOverlay = lockOverlay.Checked;
            Result.SoundsEnabled = soundsEnabled.Checked;
            Result.SoundOnExternalChanges = externalSounds.Checked;
            Result.TrayOnIcon = trayOn.Text;
            Result.TrayOffIcon = trayOff.Text;
            Result.OverlayOnImage = overlayOn.Text;
            Result.OverlayOffImage = overlayOff.Text;
            Result.MuteSound = muteSound.Text;
            Result.UnmuteSound = unmuteSound.Text;
            ResetOverlayPosition = resetRequested;
            DialogResult = DialogResult.OK;
            Close();
        }

        private TextBox AddFileRow(TableLayoutPanel grid, string label, string path, FileKind kind)
        {
            int row = grid.RowCount++;
            TextBox field = new TextBox();
            field.ReadOnly = true;
            field.Dock = DockStyle.Fill;
            field.Text = path ?? String.Empty;
            BindAccessible(field, "{0}: путь к файлу", label);
            toolTip.SetToolTip(field, field.Text);
            Button browse = MakeButton("Выбрать…");
            Button reset = MakeButton("Сброс");
            BindAccessible(browse, "{0}: выбрать файл", label);
            BindAccessible(reset, "{0}: сбросить файл", label);
            browse.Click += delegate
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = String.Format(T("Выберите файл — {0}"), T(label));
                    dialog.CheckFileExists = true;
                    dialog.CheckPathExists = true;
                    dialog.Multiselect = false;
                    dialog.Filter = kind == FileKind.Sound ? T("Звуковые файлы (WAV, MP3)") + "|*.wav;*.mp3" :
                        kind == FileKind.Icon ? T("Значки ICO (*.ico)") + "|*.ico" :
                        T("Изображения (*.png;*.bmp;*.jpg;*.jpeg;*.ico)") + "|*.png;*.bmp;*.jpg;*.jpeg;*.ico";
                    try
                    {
                        if (!String.IsNullOrEmpty(field.Text) && File.Exists(field.Text)) dialog.FileName = field.Text;
                        if (dialog.ShowDialog(this) != DialogResult.OK) return;
                        ValidateFile(dialog.FileName, kind);
                        field.Text = dialog.FileName;
                        toolTip.SetToolTip(field, field.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, T("Не удалось выбрать файл.") + "\r\n" + ex.Message,
                            T("Неподходящий файл"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };
            reset.Click += delegate { field.Text = String.Empty; toolTip.SetToolTip(field, String.Empty); };
            grid.Controls.Add(FieldLabel(label), 0, row);
            grid.Controls.Add(field, 1, row);
            grid.Controls.Add(browse, 2, row);
            grid.Controls.Add(reset, 3, row);
            return field;
        }

        private void ValidateFile(string path, FileKind kind)
        {
            if (kind == FileKind.Sound)
            {
                ValidateFileSize(new FileInfo(path).Length);
                if (!VisualAssets.IsSoundFile(path)) throw new InvalidDataException(T("Требуется читаемый файл WAV или MP3."));
                return;
            }
            using (FileStream stream = File.OpenRead(path))
            {
                ValidateFileSize(stream.Length);
                if (kind == FileKind.Icon)
                {
                    using (Icon icon = new Icon(stream))
                    {
                        if (icon.Width > 4096 || icon.Height > 4096)
                            throw new InvalidDataException(T("Максимальный размер изображения — 4096 × 4096 пикселей."));
                        using (Bitmap decoded = icon.ToBitmap()) { }
                    }
                    return;
                }
                using (Image image = Image.FromStream(stream, true, false))
                {
                    Guid format = image.RawFormat.Guid;
                    if (format != ImageFormat.Png.Guid && format != ImageFormat.Bmp.Guid &&
                        format != ImageFormat.Jpeg.Guid && format != ImageFormat.Icon.Guid)
                        throw new InvalidDataException(T("Поддерживаются только PNG, BMP, JPG и ICO."));
                    if (image.Width > 4096 || image.Height > 4096)
                        throw new InvalidDataException(T("Максимальный размер изображения — 4096 × 4096 пикселей."));
                    using (Bitmap decoded = new Bitmap(image)) { }
                }
            }
        }

        private void ValidateFileSize(long length)
        {
            if (length == 0 || length > VisualAssets.MaxAssetBytes)
                throw new InvalidDataException(T("Размер файла должен быть от 1 байта до 16 МБ."));
        }

        internal void ApplyLanguage(string value)
        {
            language = UiText.NormalizeLanguage(value);
            applyingLanguage = true;
            SuspendLayout();
            try
            {
                languageSelector.SelectedIndex = language == "ru" ? 1 : language == "es" ? 2 : 0;
                foreach (var item in originalText) item.Key.Text = T(item.Value);
                foreach (var item in accessibleText) item.Key.AccessibleName = item.Value();
                SetStyleNames(iconStyle, IconStyleNames, iconStyle.SelectedIndex);
                SetStyleNames(soundStyle, SoundStyleNames, soundStyle.SelectedIndex);
                UpdateMicrophoneChoices(SelectedDeviceId());
                UpdateHotkeyText();
            }
            finally
            {
                applyingLanguage = false;
                ResumeLayout(true);
            }
        }

        private string T(string key) { return UiText.Get(key, language); }

        private void BindText(Control control, string key)
        {
            originalText.Add(control, key);
            control.Text = T(key);
        }

        private void BindAccessible(Control control, string key, string argument = null)
        {
            Func<string> getText = delegate { return argument == null ? T(key) : String.Format(T(key), T(argument)); };
            accessibleText.Add(control, getText);
            control.AccessibleName = getText();
        }

        private TableLayoutPanel AddPage(string title)
        {
            TabPage tab = new TabPage();
            BindText(tab, title);
            tab.AutoScroll = true;
            tab.Padding = new Padding(10);
            tabs.TabPages.Add(tab);
            TableLayoutPanel page = Stack();
            tab.Controls.Add(page);
            return page;
        }

        private static TableLayoutPanel Stack() { return Grid(1); }

        private static TableLayoutPanel Grid(int columns)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.ColumnCount = columns;
            panel.RowCount = 0;
            panel.Margin = new Padding(0);
            if (columns == 1) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }

        private static TableLayoutPanel FileGrid()
        {
            TableLayoutPanel grid = Grid(4);
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return grid;
        }

        private GroupBox AddGroup(TableLayoutPanel page, string title, Control content)
        {
            GroupBox group = new GroupBox();
            BindText(group, title);
            group.AutoSize = true;
            group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            group.Dock = DockStyle.Top;
            group.Padding = new Padding(10, 8, 10, 10);
            group.Margin = new Padding(0, 0, 0, 8);
            group.Controls.Add(content);
            page.Controls.Add(group);
            return group;
        }

        private Label FieldLabel(string text)
        {
            Label label = new Label();
            BindText(label, text);
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(3, 5, 10, 5);
            return label;
        }

        private Label Hint(string text)
        {
            Label label = new Label();
            BindText(label, text);
            label.AutoSize = true;
            label.Dock = DockStyle.Fill;
            label.MaximumSize = new Size(520, 0);
            label.ForeColor = SystemColors.GrayText;
            label.Margin = new Padding(3, 4, 3, 6);
            return label;
        }

        private Button MakeButton(string text)
        {
            Button button = new Button();
            BindText(button, text);
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(64, 27);
            button.Margin = new Padding(3);
            button.Padding = new Padding(5, 0, 5, 0);
            return button;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResetSoundPreview();
                SetPreviewImage(activePreview, null);
                SetPreviewImage(mutedPreview, null);
                toolTip.Dispose();
            }
            base.Dispose(disposing);
            if (disposing) dialogFont.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int key);

        private enum FileKind { Icon, Image, Sound }

        private sealed class DeviceChoice
        {
            public readonly string Id;
            private readonly string name;
            public DeviceChoice(string id, string name) { Id = id; this.name = name; }
            public override string ToString() { return name; }
        }
    }
}
