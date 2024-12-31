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

namespace Windows.Shell
{
    class TopDragWindow : NonClientWindow
    {
        public TopDragWindow(RECT position, HWND parent)
            : base(position, parent,
                  WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP,
                  WINDOW_STYLE.WS_CHILD, true)
        { }

        protected override LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_NCHITTEST:
                    return HandleHitTest(hwnd, lParam);
                case PInvoke.WM_NCLBUTTONDOWN:
                case PInvoke.WM_NCLBUTTONDBLCLK:
                case PInvoke.WM_NCRBUTTONDOWN:
                case PInvoke.WM_NCRBUTTONDBLCLK:
                case PInvoke.WM_NCMBUTTONDOWN:
                case PInvoke.WM_NCMBUTTONDBLCLK:
                case PInvoke.WM_NCXBUTTONDOWN:
                case PInvoke.WM_NCXBUTTONDBLCLK:
                    PInvoke.SendMessage(ParentHandle, msg, wParam, 0);
                    return new LRESULT(0);
            }
            return base.WndProc(hwnd, msg, wParam, lParam);
        }

        private LRESULT HandleHitTest(HWND hwnd, LPARAM lParam)
        {
            var _pos = PInvoke.PARAM.ToPoint(lParam);
            var pos = WindowHelper.PointToClient(hwnd, _pos);
            var size = WindowHelper.GetWindowBorderSize(WindowHelper.GetDpiForWindow(this.ParentHandle));

            if (pos.X < size.Width)
            {
                return new LRESULT((nint)PInvoke.HTTOPLEFT);
            }
            PInvoke.GetClientRect(hwnd, out var rect);
            if (pos.X > rect.Width - size.Width)
            {
                return new LRESULT((nint)PInvoke.HTTOPRIGHT);
            }
            return new LRESULT((nint)PInvoke.HTTOP);
        }
    }
}
