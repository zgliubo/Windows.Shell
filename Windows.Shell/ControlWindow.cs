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
    class ControlWindow : NonClientWindow
    {
        public const int ButtonHeight = 32;
        public const int ButtonWidth = 46;

        public ControlWindow(RECT position, HWND parent) : base(position, parent)
        {
            var style = (uint)PInvoke.GetWindowLong(this.Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            style |= (uint)WINDOW_STYLE.WS_MINIMIZEBOX;
            PInvoke.SetWindowLong(this.Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)style);
        }

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
                    //PInvoke.SendMessage(ParentHandle, msg, wParam, 0);
                    return new LRESULT(0);

            }
            return base.WndProc(hwnd, msg, wParam, lParam);
        }

        private LRESULT HandleHitTest(HWND hwnd, LPARAM lParam)
        {
            var style = (uint)PInvoke.GetWindowLong(this.ParentHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            if ((style & (uint)WINDOW_STYLE.WS_THICKFRAME) == 0) // NoResize
            {
                return new LRESULT((nint)PInvoke.HTCLOSE);
            }

            var _pos = PInvoke.PARAM.ToPoint(lParam);
            var pos = WindowHelper.PointToClient(hwnd, _pos);
            PInvoke.GetClientRect(hwnd, out var rect);

            if (pos.X < rect.Width / 3)
            {
                return new LRESULT((nint)PInvoke.HTMINBUTTON);
            }

            if (pos.X > rect.Width * 2 / 3)
            {
                return new LRESULT((nint)PInvoke.HTCLOSE);
            }

            return new LRESULT((nint)PInvoke.HTMAXBUTTON);
        }
    }
}
