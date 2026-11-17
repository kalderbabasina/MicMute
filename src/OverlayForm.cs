using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MicMute
{
    internal sealed class OverlayForm : Form
    {
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destination, ref Point position, ref Size size, IntPtr source, ref Point sourcePoint, uint colorKey, ref BlendFunction blend, uint flags);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BlendFunction { public byte Operation, Flags, Alpha, Format; }
        private AppConfig config;
        private VisualAssets assets;
        private bool? muted;
        private Point startCursor, startLocation;
        private int startSize;
        private bool resizing, dragging;
        private readonly ContextMenuStrip menu;
        private readonly ToolTip hint = new ToolTip();
        public event EventHandler ToggleRequested;
        public event EventHandler PlacementChanged;

        public OverlayForm(ContextMenuStrip menu)
        {
            this.menu = menu;
            Text = "MicMute indicator";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(25, 27, 32);
            AccessibleName = "Состояние микрофона";
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var p = base.CreateParams; p.ExStyle |= 0x08080080; return p; }
        }

        public void ApplyConfig(AppConfig newConfig, VisualAssets newAssets, bool resetPosition)
        {
            config = newConfig; assets = newAssets;
            Text = T("Индикатор MicMute");
            AccessibleName = T("Состояние микрофона");
            Size = new Size(config.OverlaySize, config.OverlaySize);
            Cursor = config.LockOverlay ? Cursors.Hand : Cursors.SizeAll;
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            var position = new Point(config.OverlayX, config.OverlayY);
            Rectangle target = new Rectangle(position, Size);
            bool reachable = false;
            if (!resetPosition && position.X != int.MinValue && position.Y != int.MinValue)
                foreach (Screen screen in Screen.AllScreens)
                {
                    Rectangle visible = Rectangle.Intersect(screen.WorkingArea, target);
                    if (visible.Width >= Math.Min(Width, 32) && visible.Height >= Math.Min(Height, 32)) { reachable = true; break; }
                }
            Location = reachable ? position : new Point(area.Right - Width - 24, area.Top + 48);
            UpdateShape();
            if (config.OverlayEnabled) { Show(); RaiseAboveWindows(); } else Hide();
            RenderSurface();
        }

        public void ApplyStatus(bool? value)
        {
            muted = value;
            string state = T(!value.HasValue ? "Микрофон недоступен" : value.Value ? "Микрофон выключен" : "Микрофон включён");
            AccessibleDescription = state;
            hint.SetToolTip(this, state + T("\nКлик — mute/unmute · Shift + перетаскивание — размер\nПравая кнопка — настройки"));
            UpdateShape();
            RenderSurface();
        }

        private Image CurrentImage { get { return assets == null || !muted.HasValue ? null : muted.Value ? assets.MutedImage : assets.ActiveImage; } }
        private string T(string text) { return UiText.Get(text, config == null ? "en" : config.AppLanguage); }

        private void UpdateShape()
        {
            Region previous = Region;
            // The layered surface retains PNG alpha, including antialiased edges.
            using (var path = new GraphicsPath())
            {
                if (CurrentImage == null && config.IconStyle != 2) path.AddEllipse(0, 0, Width, Height);
                else path.AddRectangle(ClientRectangle);
                Region = new Region(path);
            }
            if (previous != null) previous.Dispose();
        }

        public void RaiseAboveWindows()
        {
            if (Visible && IsHandleCreated && !IsDisposed && !menu.Visible)
                SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (config != null)
                BeginInvoke((MethodInvoker)delegate { if (!IsDisposed) { RenderSurface(); RaiseAboveWindows(); } });
        }
        protected override void OnShown(EventArgs e) { base.OnShown(e); RenderSurface(); RaiseAboveWindows(); }
        private void RenderSurface()
        {
            if (!IsHandleCreated || config == null || assets == null || !Visible) return;
            using (var bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    Image image = CurrentImage;
                    if (image == null) VisualAssets.DrawStatus(graphics, ClientRectangle, muted, config.IconStyle);
                    else
                    {
                        float scale = Math.Min((float)Width / image.Width, (float)Height / image.Height);
                        int w = Math.Max(1, (int)(image.Width * scale)), h = Math.Max(1, (int)(image.Height * scale));
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(image, new Rectangle((Width - w) / 2, (Height - h) / 2, w, h));
                    }
                }
                IntPtr screenDC = GetDC(IntPtr.Zero), memoryDC = IntPtr.Zero, nativeBitmap = IntPtr.Zero, previous = IntPtr.Zero;
                try
                {
                    memoryDC = CreateCompatibleDC(screenDC);
                    nativeBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                    previous = SelectObject(memoryDC, nativeBitmap);
                    Point source = Point.Empty, position = Location;
                    Size size = Size;
                    var blend = new BlendFunction { Alpha = (byte)(config.OverlayOpacity * 255 / 100), Format = 1 };
                    if (!UpdateLayeredWindow(Handle, screenDC, ref position, ref size, memoryDC, ref source, 0, ref blend, 2))
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
                finally
                {
                    if (previous != IntPtr.Zero) SelectObject(memoryDC, previous);
                    if (nativeBitmap != IntPtr.Zero) DeleteObject(nativeBitmap);
                    if (memoryDC != IntPtr.Zero) DeleteDC(memoryDC);
                    if (screenDC != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screenDC);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            startCursor = Cursor.Position; startLocation = Location; startSize = Width;
            dragging = false; resizing = (ModifierKeys & Keys.Shift) != 0; Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!Capture || (e.Button & MouseButtons.Left) == 0 || config == null) return;
            Point delta = new Point(Cursor.Position.X - startCursor.X, Cursor.Position.Y - startCursor.Y);
            if (!dragging && Math.Abs(delta.X) < SystemInformation.DragSize.Width / 2 && Math.Abs(delta.Y) < SystemInformation.DragSize.Height / 2) return;
            dragging = true;
            if (config.LockOverlay) return;
            if (resizing)
            {
                int change = Math.Abs(delta.X) > Math.Abs(delta.Y) ? delta.X : delta.Y;
                int size = Math.Max(40, Math.Min(256, startSize + change));
                if (Width != size) { Size = new Size(size, size); UpdateShape(); RenderSurface(); }
            }
            else Location = new Point(startLocation.X + delta.X, startLocation.Y + delta.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e); Capture = false;
            if (e.Button == MouseButtons.Right) menu.Show(Cursor.Position);
            else if (e.Button == MouseButtons.Left)
            {
                if (dragging && !config.LockOverlay)
                {
                    config.OverlayX = Left; config.OverlayY = Top; config.OverlaySize = Width;
                    if (PlacementChanged != null) PlacementChanged(this, EventArgs.Empty);
                }
                else if (!dragging && !resizing && ToggleRequested != null) ToggleRequested(this, EventArgs.Empty);
            }
        }

        protected override void Dispose(bool disposing) { if (disposing) hint.Dispose(); base.Dispose(disposing); }
    }
}
