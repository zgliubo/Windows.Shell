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
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Win32.System.Registry;

namespace Windows.Shell
{
    class BorderWindow : ResizeBorderWindow
    {
        static BorderWindow()
        {
            RegNotifyChangeKeyValue();
        }

        private bool isActive;
        private static readonly Color activeColor = Color.FromArgb(112, 112, 112);
        private static readonly Color inactiveColor = Color.FromArgb(170, 170, 170);
        private static Color dwmColor;
        private static bool useDWMColor;

        public BorderWindow(RECT position, HWND parent, Dock dock)
            : base(position, parent, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW,
                  WINDOW_STYLE.WS_POPUP, false)
        {
            _Dock = dock;
        }

        public static List<BorderWindow> CreateWindows(RECT position, HWND parent)
        {
            return new List<BorderWindow>
            {
                new(position, parent, Dock.Left),
                new(position, parent, Dock.Top),
                new(position, parent, Dock.Right),
                new(position, parent, Dock.Bottom),
            };
        }

        protected override LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_ERASEBKGND:
                    HandleBackgroud(hwnd, wParam);
                    return new LRESULT(1);
                case PInvoke.WM_DWMCOLORIZATIONCOLORCHANGED:
                    if (useDWMColor) dwmColor = GetColorizationColor();
                    break;
            }
            return base.WndProc(hwnd, msg, wParam, lParam);
        }

        public override void UpdatePositionWidthActive(RECT position, bool isActive)
        {
            this.isActive = isActive;
            UpdatePosition(position);
            PInvoke.InvalidateRect(Handle, (RECT?)null, true);
        }

        protected override Size HandlePosition(ref RECT position, int lineWidth, uint dpi)
        {
            position.top += lineWidth;
            return new Size(lineWidth, lineWidth);
        }

        private void HandleBackgroud(HWND hwnd, WPARAM wParam)
        {
            var hdc = new HDC((nint)wParam.Value);
            PInvoke.GetClientRect(hwnd, out var rect);
            var color = isActive ? (useDWMColor ? dwmColor : activeColor) : inactiveColor;
            var hbr = PInvoke.CreateSolidBrush_SafeHandle(WindowHelper.ColorToCOLORREF(color));
            PInvoke.FillRect(hdc, rect, hbr);
        }

        private static Color GetColorizationColor()
        {
            PInvoke.DwmGetColorizationColor(out var color, out _);
            return Color.FromArgb(
                (byte)((color >> 24) & 0xFF),
                (byte)((color >> 16) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)(color & 0xFF));
        }

        private static void RegNotifyChangeKeyValue()
        {
            var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM", false)!;
            if (IsWindowPrevalenceAccentColor(key))
            { 
                useDWMColor = true;
                dwmColor = GetColorizationColor();
            }

            if (!OsVersion.IsWindows10_1809OrGreater)
            {
                return;
            }

            Task.Run(() =>
            {
                while (true)
                {
                    var result = PInvoke.RegNotifyChangeKeyValue(key.Handle, false, REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET, null, false);
                    if (result != WIN32_ERROR.NO_ERROR)
                    {
                        break;
                    }

                    if (IsWindowPrevalenceAccentColor(key) != useDWMColor)
                    {
                        useDWMColor = !useDWMColor;
                        dwmColor = GetColorizationColor();
                    }
                }
            });
        }

        private static bool IsWindowPrevalenceAccentColor(RegistryKey key)
        {
            // 个性化/颜色/"标题栏和边框"

            if (!OsVersion.IsWindows10_1809OrGreater)
            {
                //win10 1809 之前没有此设置, 边框颜色为主题色
                return true;
            }

            if (key.GetValueNames().Contains("ColorPrevalence"))
            {
                int? colorPrevalence = (int?)key.GetValue("ColorPrevalence");

                return colorPrevalence == 1;
            }

            return false;
        }
    }
}
