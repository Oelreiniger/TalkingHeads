using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TalkingHeads.Commands
{
    class WindowsTools
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(
            IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
            IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;
        const int SRCCOPY = 0x00CC0020;

        public static void CaptureScreen(string outputPath)
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            using Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(bmp);
            IntPtr hdcDest = g.GetHdc();

            IntPtr hdcSrc = GetDC(IntPtr.Zero);
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
            ReleaseDC(IntPtr.Zero, hdcSrc);

            g.ReleaseHdc(hdcDest);
            bmp.Save(outputPath, ImageFormat.Png);
        }
    }
}
