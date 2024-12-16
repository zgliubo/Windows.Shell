using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using System.Drawing;

namespace Windows.Shell
{
    class WindowHelper
    {
        public static Point PointToClient(HWND hwnd, Point p)
        {
            Span<Point> ps = new Point[1] { p };
            PInvoke.MapWindowPoints(HWND.Null, hwnd, ps);
            return ps[0];
        }

        public static Point PointToScreen(HWND hwnd, Point p)
        {
            Span<Point> ps = new Point[1] { p };
            PInvoke.MapWindowPoints(hwnd, HWND.Null, ps);
            return ps[0];
        }

        public static Size GetWindowBorderSize(uint dpi)
        {
            if (!OsVersion.IsWindows10_1607OrGreater)
            {
                var scale = (double)dpi / PInvoke.USER_DEFAULT_SCREEN_DPI;
                var value = (int)Math.Round(4 * scale) + 3 + (int)Math.Round(1 * scale);
                return new Size(value, value);
            }
            else
            {
                var padded_border = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi);
                var border_x = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXFRAME, dpi) + padded_border;
                var border_y = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CYFRAME, dpi) + padded_border;
                return new Size(border_x, border_y);
            }
        }

        public static uint GetDpiForSystem()
        {
            // This will only change when the first call to set the process DPI awareness is made. Multiple calls to
            // set the DPI have no effect after making the first call. Depending on what the DPI awareness settings are
            // we'll get either the actual DPI of the primary display at process startup or the default LogicalDpi;

            if (!OsVersion.IsWindows10_1607OrGreater)
            {
                var dc = PInvoke.GetDC(HWND.Null);
                var dpi = PInvoke.GetDeviceCaps(dc, GET_DEVICE_CAPS_INDEX.LOGPIXELSX);
                _ = PInvoke.ReleaseDC(HWND.Null, dc);
                return (uint)dpi;
            }

            // This avoids needing to create a DC
            return PInvoke.GetDpiForSystem();
        }

        public static uint GetDpiForWindow(HWND hwnd)
        {
            if (OsVersion.IsWindows10_1607OrGreater)
            {
                return PInvoke.GetDpiForWindow(hwnd);
            }

            if (OsVersion.IsWindows8_1OrGreater)
            {
                var hMonitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
                if (hMonitor != 0)
                {
                    if (PInvoke.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _).Succeeded)
                    {
                        return dpiX;
                    }
                }
            }

            return GetDpiForSystem();
        }
    }
}
