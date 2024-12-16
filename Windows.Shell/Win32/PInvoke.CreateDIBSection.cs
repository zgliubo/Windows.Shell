using Windows.Win32.Foundation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32
{
    namespace Graphics.Gdi
    {
        internal struct BITMAPINFOHEADER
        {
            internal int biSize;

            internal int biWidth;

            internal int biHeight;

            internal short biPlanes;

            internal short biBitCount;

            internal uint biCompression;

            internal uint biSizeImage;

            internal int biXPelsPerMeter;

            internal int biYPelsPerMeter;

            internal uint biClrUsed;

            internal uint biClrImportant;

            internal static BITMAPINFOHEADER Default
            {
                get
                {
                    BITMAPINFOHEADER result = default;
                    result.biSize = Marshal.SizeOf<BITMAPINFOHEADER>();
                    result.biPlanes = 1;
                    return result;
                }
            }
        }

        internal struct BITMAPINFO
        {
            internal int biSize;

            internal int biWidth;

            internal int biHeight;

            internal short biPlanes;

            internal short biBitCount;

            internal uint biCompression;

            internal uint biSizeImage;

            internal int biXPelsPerMeter;

            internal int biYPelsPerMeter;

            internal uint biClrUsed;

            internal uint biClrImportant;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
            internal byte[] bmiColors;

            internal static BITMAPINFO Default
            {
                get
                {
                    BITMAPINFO result = default;
                    result.biSize = Marshal.SizeOf<BITMAPINFOHEADER>();
                    result.biPlanes = 1;
                    return result;
                }
            }
        }
    }

    internal static partial class PInvoke
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateDIBSection(IntPtr hdc, ref Graphics.Gdi.BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
    }
}
