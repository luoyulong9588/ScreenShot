using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenShot
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            SetProcessDPIAware();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _messageWindow = new MessageWindow();
            _messageWindow.HotKeyPressed += StartCapture;
            CreateTrayIcon();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _messageWindow?.Dispose();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.OnExit(e);
        }

        private void CreateTrayIcon()
        {
            var icon = CreateDefaultIcon();
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = icon,
                Text = "截图工具",
                Visible = true
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("截图", null, (s, ev) => StartCapture());
            menu.Items.Add("退出", null, (s, ev) => Shutdown());
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.MouseClick += (s, ev) =>
            {
                if (ev.Button == System.Windows.Forms.MouseButtons.Left)
                    StartCapture();
            };
        }

        private System.Windows.Forms.NotifyIcon _trayIcon;
        private MessageWindow _messageWindow;
        private bool _isCapturing;

        private void StartCapture()
        {
            if (_isCapturing) return;
            _isCapturing = true;

            try
            {
                var screenBitmap = ScreenHelper.CapturePrimaryScreen();
                var overlay = new CaptureOverlayWindow(screenBitmap);
                overlay.ShowDialog();

                if (overlay.SelectedRegion.HasValue)
                {
                    var region = overlay.SelectedRegion.Value;
                    var cropped = ScreenHelper.CropImage(screenBitmap, region);
                    var annotation = new AnnotationWindow(cropped);
                    annotation.ShowDialog();
                }
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private static System.Drawing.Icon CreateDefaultIcon()
        {
            var bmp = new System.Drawing.Bitmap(32, 32);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215)))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2f))
                {
                    g.FillRectangle(brush, 2, 2, 28, 28);
                    g.DrawRectangle(pen, 8, 8, 16, 16);
                }
            }
            IntPtr hIcon = bmp.GetHicon();
            var icon = System.Drawing.Icon.FromHandle(hIcon);
            return icon;
        }

        private class MessageWindow : IDisposable
        {
            public event Action HotKeyPressed;
            private const int WM_HOTKEY = 0x0312;
            private readonly System.Windows.Interop.HwndSource _source;
            private readonly IntPtr _hwnd;

            public MessageWindow()
            {
                var param = new System.Windows.Interop.HwndSourceParameters("ScreenShot_Hotkey")
                {
                    Width = 0,
                    Height = 0,
                    PositionX = 0,
                    PositionY = 0,
                    WindowStyle = 0
                };
                _source = new System.Windows.Interop.HwndSource(param);
                _hwnd = _source.Handle;
                _source.AddHook(WndProc);

                NativeMethods.RegisterHotKey(_hwnd, NativeMethods.HOTKEY_ID,
                    NativeMethods.MOD_CTRL | NativeMethods.MOD_SHIFT, 0x41);
            }

            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == WM_HOTKEY && wParam.ToInt32() == NativeMethods.HOTKEY_ID)
                {
                    HotKeyPressed?.Invoke();
                    handled = true;
                }
                return IntPtr.Zero;
            }

            public void Dispose()
            {
                NativeMethods.UnregisterHotKey(_hwnd, NativeMethods.HOTKEY_ID);
                _source?.RemoveHook(WndProc);
                _source?.Dispose();
            }
        }
    }
}
