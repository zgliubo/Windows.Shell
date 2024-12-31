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
    class ResizeWindow : ResizeBorderWindow
    {
        private readonly bool showGlow;
        private bool isActive;

        public ResizeWindow(RECT position, HWND parent, Dock dock, bool showGlow) 
            : base(position, parent,
                  WINDOW_EX_STYLE.WS_EX_LAYERED,
                  WINDOW_STYLE.WS_POPUP, !showGlow)
        {
            _Dock = dock;
            this.showGlow = showGlow;
        }

        public static List<ResizeWindow> CreateWindows(RECT position, HWND parent, bool showGlow)
        {
            return new List<ResizeWindow>
            {
                new(position, parent, Dock.Left, showGlow),
                new(position, parent, Dock.Top, showGlow),
                new(position, parent, Dock.Right, showGlow),
                new(position, parent, Dock.Bottom, showGlow),
            };
        }

        public static void OnDwmChanged(List<ResizeWindow> windows, RECT position, HWND parent, bool dwmEnabled, bool isActive)
        {
            if (dwmEnabled)
            {
                _ = PInvoke.SetWindowRgn(parent, HRGN.Null, true);
            }
            else
            {
                var hRgn = PInvoke.CreateRectRgnIndirect(position);
                _ = PInvoke.SetWindowRgn(parent, hRgn, true);
            }

            var showGlow = !windows.First().showGlow;

            windows.ForEach(x => x.Close());
            windows.Clear();
            windows.AddRange(CreateWindows(default, parent, showGlow));
            windows.ForEach(x =>
            {
                x.UpdatePositionWidthActive(position, isActive);
                x.Show();
            });
        }

        public override void UpdatePositionWidthActive(RECT position, bool isActive)
        {
            this.isActive = isActive;
            this.UpdatePosition(position);
        }

        protected override Size HandlePosition(ref RECT position, int lineWidth, uint dpi)
        {
            position.left += lineWidth;
            position.top += lineWidth;
            position.right -= lineWidth;
            position.bottom -= lineWidth;
            return WindowHelper.GetWindowBorderSize(dpi);
        }

        protected override void OnUpdatedPosition(RECT position)
        {
            if (showGlow)
            {
                GlowHelper.RenderLayeredWindow(this.Handle, position, _Dock, isActive);
            }
        }
    }
}
