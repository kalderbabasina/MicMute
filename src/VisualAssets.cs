using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

namespace MicMute
{
    // Decoded once when settings change, never on a polling tick. No file stays locked.
    internal sealed class VisualAssets : IDisposable
    {
        public Icon ActiveIcon, MutedIcon, UnknownIcon;
        public Image ActiveImage, MutedImage;
        private readonly AudioFeedback audio;
        public const int MaxAssetBytes = 16 * 1024 * 1024;

        public VisualAssets(AppConfig config)
        {
            ActiveIcon = LoadIcon(config.TrayOnIcon) ?? CreateStatusIcon(false, config.IconStyle);
            MutedIcon = LoadIcon(config.TrayOffIcon) ?? CreateStatusIcon(true, config.IconStyle);
            UnknownIcon = CreateStatusIcon(null, config.IconStyle);
            ActiveImage = LoadImage(config.OverlayOnImage);
            MutedImage = LoadImage(config.OverlayOffImage);
            audio = new AudioFeedback(config);
        }

        internal static Icon LoadIcon(string path)
        {
            try
            {
                if (!IsSafeFile(path) || !string.Equals(Path.GetExtension(path), ".ico", StringComparison.OrdinalIgnoreCase)) return null;
                using (var stream = File.OpenRead(path))
                using (var icon = new Icon(stream)) return (Icon)icon.Clone();
            }
            catch (Exception ex) { if (ex is OutOfMemoryException || ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is System.Security.SecurityException) return null; throw; }
        }

        internal static Image LoadImage(string path)
        {
            try
            {
                if (!IsSafeFile(path)) return null;
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".ico")
                {
                    using (Icon icon = LoadIcon(path)) return icon == null ? null : icon.ToBitmap();
                }
                if (ext != ".png" && ext != ".bmp" && ext != ".jpg" && ext != ".jpeg") return null;
                using (var stream = File.OpenRead(path))
                using (Image image = Image.FromStream(stream, true, true))
                {
                    if (image.Width > 4096 || image.Height > 4096) return null;
                    return new Bitmap(image);
                }
            }
            catch (Exception ex) { if (ex is OutOfMemoryException || ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is System.Security.SecurityException) return null; throw; }
        }

        internal static bool IsSafeFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var file = new FileInfo(path);
            return file.Exists && file.Length > 0 && file.Length <= MaxAssetBytes;
        }

        internal static bool IsSoundFile(string path) { return AudioFeedback.IsSoundFile(path); }
        public void Play(bool muted) { audio.Play(muted); }

        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

        internal static Icon CreateStatusIcon(bool? muted, int style = 0)
        {
            using (var bitmap = new Bitmap(64, 64))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                DrawStatus(g, new Rectangle(0, 0, 64, 64), muted, style);
                IntPtr handle = bitmap.GetHicon();
                try { using (Icon borrowed = Icon.FromHandle(handle)) return (Icon)borrowed.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }

        internal static void DrawStatus(Graphics g, Rectangle bounds, bool? muted, int style = 0)
        {
            GraphicsState saved = g.Save();
            try
            {
                g.TranslateTransform(bounds.X, bounds.Y);
                g.ScaleTransform(bounds.Width / 64f, bounds.Height / 64f);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color fill = !muted.HasValue ? Color.FromArgb(108, 113, 124) : muted.Value ? Color.FromArgb(223, 66, 79) : Color.FromArgb(21, 157, 116);
                Color ink = style == 1
                    ? (!muted.HasValue ? Color.Gainsboro : muted.Value ? Color.FromArgb(255, 136, 145) : Color.FromArgb(103, 238, 184))
                    : Color.White;
                Color background = style == 1 ? Color.FromArgb(26, 32, 39) : fill;
                if (style == 2)
                {
                    DrawPixel(g, fill, muted);
                    return;
                }
                using (var brush = new SolidBrush(background)) g.FillEllipse(brush, 2, 2, 60, 60);
                using (var border = new Pen(style == 1 ? ink : Color.FromArgb(225, 235, 240), style == 1 ? 3f : 1.5f))
                    g.DrawEllipse(border, 4, 4, 56, 56);
                using (var pen = new Pen(ink, 3.5f))
                using (var brush = new SolidBrush(ink))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    g.FillEllipse(brush, 26, 14, 12, 12);
                    g.FillRectangle(brush, 26, 20, 12, 13);
                    g.FillEllipse(brush, 26, 26, 12, 12);
                    g.DrawArc(pen, 20, 21, 24, 23, 0, 180);
                    g.DrawLine(pen, 32, 44, 32, 50);
                    g.DrawLine(pen, 25, 50, 39, 50);
                    if (muted == true) g.DrawLine(pen, 17, 16, 47, 48);
                    if (!muted.HasValue)
                    {
                        using (var badge = new SolidBrush(background)) g.FillEllipse(badge, 38, 36, 20, 22);
                        using (var font = new Font(SystemFonts.MessageBoxFont.FontFamily, 16, FontStyle.Bold, GraphicsUnit.Pixel)) g.DrawString("?", font, brush, 40, 36);
                    }
                }
            }
            finally { g.Restore(saved); }
        }

        private static void DrawPixel(Graphics g, Color color, bool? muted)
        {
            g.SmoothingMode = SmoothingMode.None;
            using (var frame = new SolidBrush(Color.FromArgb(26, 32, 39)))
            using (var tile = new SolidBrush(color))
            using (var ink = new SolidBrush(Color.White))
            {
                g.FillRectangle(frame, 8, 4, 48, 56); g.FillRectangle(frame, 4, 8, 56, 48);
                g.FillRectangle(tile, 8, 8, 48, 48);
                g.FillRectangle(ink, 28, 16, 8, 4); g.FillRectangle(ink, 24, 20, 16, 16);
                g.FillRectangle(ink, 28, 36, 8, 4);
                g.FillRectangle(ink, 20, 32, 4, 12); g.FillRectangle(ink, 40, 32, 4, 12);
                g.FillRectangle(ink, 24, 44, 16, 4); g.FillRectangle(ink, 28, 48, 8, 4);
                if (muted == true)
                {
                    for (int n = 0; n < 8; n++)
                    {
                        g.FillRectangle(frame, 16 + n * 4, 12 + n * 4, 8, 8);
                        g.FillRectangle(ink, 16 + n * 4, 12 + n * 4, 4, 4);
                    }
                }
                else if (!muted.HasValue)
                {
                    g.FillRectangle(frame, 36, 32, 20, 24);
                    g.FillRectangle(ink, 40, 36, 12, 4); g.FillRectangle(ink, 48, 40, 4, 4);
                    g.FillRectangle(ink, 44, 44, 4, 4); g.FillRectangle(ink, 44, 52, 4, 4);
                }
            }
        }

        public void Dispose()
        {
            ActiveIcon.Dispose(); MutedIcon.Dispose(); UnknownIcon.Dispose();
            if (ActiveImage != null) ActiveImage.Dispose();
            if (MutedImage != null) MutedImage.Dispose();
            audio.Dispose();
        }
    }
}
