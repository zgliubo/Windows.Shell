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

namespace Windows.Shell
{
    class ResizeWindow : NonClientWindow
    {
        public enum Dock
        {
            Left,
            Top,
            Right,
            Bottom
        }

        private readonly Dock _dock;
        private readonly bool showGlow;
        private bool isActive;

        public ResizeWindow(RECT position, HWND parent, Dock dock, bool showGlow) : base(position, parent, false, !showGlow)
        {
            _dock = dock;
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
                x.UpdateGlow(position, isActive);
                x.Show();
            });
        }

        protected override LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_MOUSEACTIVATE:
                    return new LRESULT((nint)PInvoke.MA_NOACTIVATE);
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

        public void UpdateGlow(RECT position, bool isActive)
        {
            this.isActive = isActive;
            UpdatePosition(position);
        }

        public override void UpdatePosition(RECT position)
        {
            position.left++;
            position.top++;
            position.right--;
            position.bottom--;

            var ps = new Point[2] { new(position.left, position.top), new(position.right, position.bottom) };
            PInvoke.MapWindowPoints(ParentHandle, HWND.Null, ps);
            position = new RECT(ps[0].X, ps[0].Y, ps[1].X, ps[1].Y);
            var size = WindowHelper.GetWindowBorderSize(WindowHelper.GetDpiForWindow(ParentHandle));

            switch (_dock)
            {
                case Dock.Left:
                    position.left -= size.Width;
                    position.right = position.left + size.Width;
                    break;
                case Dock.Top:
                    position.left -= size.Width;
                    position.top -= size.Height;
                    position.right += size.Width;
                    position.bottom = position.top + size.Height;
                    break;
                case Dock.Right:
                    position.left = position.right;
                    position.right = position.left + size.Width;
                    break;
                case Dock.Bottom:
                    position.left -= size.Width;
                    position.top = position.bottom;
                    position.right += size.Width;
                    position.bottom = position.top + size.Height;
                    break;
            }

            if (showGlow)
            {
                GlowHelper.RenderLayeredWindow(this.Handle, position, _dock, isActive);
            }

            base.UpdatePosition(position);
        }

        private LRESULT HandleHitTest(HWND hwnd, LPARAM lParam)
        {
            var style = (uint)PInvoke.GetWindowLong(this.ParentHandle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            if ((style & (uint)WINDOW_STYLE.WS_THICKFRAME) == 0) // NoResize
            {
                return new LRESULT((nint)PInvoke.HTNOWHERE);
            }

            var _pos = PInvoke.PARAM.ToPoint(lParam);
            var pos = WindowHelper.PointToClient(hwnd, _pos);
            var size = WindowHelper.GetWindowBorderSize(WindowHelper.GetDpiForWindow(ParentHandle));
            PInvoke.GetClientRect(hwnd, out var rect);

            if (_dock == Dock.Left)
            {
                if (pos.Y < size.Height)
                {
                    return new LRESULT((nint)PInvoke.HTTOPLEFT);
                }
                if (pos.Y > rect.Height - size.Height)
                {
                    return new LRESULT((nint)PInvoke.HTBOTTOMLEFT);
                }
                return new LRESULT((nint)PInvoke.HTLEFT);
            }

            if (_dock == Dock.Right)
            {
                if (pos.Y < size.Height)
                {
                    return new LRESULT((nint)PInvoke.HTTOPRIGHT);
                }
                if (pos.Y > rect.Height - size.Height)
                {
                    return new LRESULT((nint)PInvoke.HTBOTTOMRIGHT);
                }
                return new LRESULT((nint)PInvoke.HTRIGHT);
            }

            if (_dock == Dock.Top)
            {
                if (pos.X < size.Width * 2)
                {
                    return new LRESULT((nint)PInvoke.HTTOPLEFT);
                }

                if (pos.X > rect.Width - size.Width * 2)
                {
                    return new LRESULT((nint)PInvoke.HTTOPRIGHT);
                }

                return new LRESULT((nint)PInvoke.HTTOP);
            }

            if (_dock == Dock.Bottom)
            {
                if (pos.X < size.Width * 2)
                {
                    return new LRESULT((nint)PInvoke.HTBOTTOMLEFT);
                }

                if (pos.X > rect.Width - size.Width * 2)
                {
                    return new LRESULT((nint)PInvoke.HTBOTTOMRIGHT);
                }

                return new LRESULT((nint)PInvoke.HTBOTTOM);
            }

            return new LRESULT((nint)PInvoke.HTNOWHERE);
        }
    }
}
