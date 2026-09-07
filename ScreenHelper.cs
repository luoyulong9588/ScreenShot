using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GdiPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace ScreenShot
{
    internal static class ScreenHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int HORZRES = 8;
        private const int VERTRES = 10;

        public static BitmapSource CapturePrimaryScreen()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int width = GetDeviceCaps(hdc, HORZRES);
            int height = GetDeviceCaps(hdc, VERTRES);
            ReleaseDC(IntPtr.Zero, hdc);

            using (var bmp = new Bitmap(width, height, GdiPixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height),
                        CopyPixelOperation.SourceCopy);
                }

                var data = bmp.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    GdiPixelFormat.Format32bppArgb);

                try
                {
                    var pixels = new byte[data.Stride * data.Height];
                    Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                    var result = BitmapSource.Create(
                        width, height, 96, 96,
                        PixelFormats.Bgra32, null,
                        pixels, data.Stride);
                    result.Freeze();
                    return result;
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        public static BitmapSource CropImage(BitmapSource source, Int32Rect rect)
        {
            return new CroppedBitmap(source, rect);
        }
    }
}
