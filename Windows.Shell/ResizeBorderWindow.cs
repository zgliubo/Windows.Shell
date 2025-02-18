﻿using System;
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
using Dock = Windows.Shell.WindowEdge.Dock;
using EdgeMode = Windows.Shell.WindowEdge.EdgeMode;

namespace Windows.Shell
{
    class ResizeBorderWindow : NonClientWindow
    {
        private readonly Dock _dock;
        private readonly EdgeMode _mode;
        private bool isActive;

        public ResizeBorderWindow(RECT position, HWND parent, Dock dock, EdgeMode mode)
           : base(position, parent,
                 WINDOW_EX_STYLE.WS_EX_LAYERED,
                 WINDOW_STYLE.WS_POPUP, setLayered: mode == EdgeMode.Resize)
        {
            _dock = dock;
            _mode = mode;
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

        public void UpdatePositionWidthActive(RECT position, bool isActive)
        {
            this.isActive = isActive;
            this.UpdatePosition(position);
        }

        public override void UpdatePosition(RECT position)
        {
            HandlePosition(ref position, out var size, out var lineWidth);

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

            RenderGlow(position, lineWidth);

            base.UpdatePosition(position);
        }

        private void HandlePosition(ref RECT position, out Size size, out int lineWidth)
        {
            var dpi = WindowHelper.GetDpiForWindow(ParentHandle);
            var bw = (int)Math.Round(1.0 * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI); //边框线宽度 1px

            lineWidth = bw;

            if (_mode == EdgeMode.Border)
            {
                size = new Size(bw, bw);
            }
            else
            {
                size = WindowHelper.GetWindowBorderSize(dpi);
            }

            var ps = new Point[2] { new(position.left, position.top), new(position.right, position.bottom) };
            PInvoke.MapWindowPoints(ParentHandle, HWND.Null, ps);
            position = new RECT(ps[0].X, ps[0].Y, ps[1].X, ps[1].Y);
        }

        private void RenderGlow(RECT position, int lineWidth)
        {
            if (_mode == EdgeMode.Resize)
            {
                return;
            }
            if (_mode == EdgeMode.BorderResizeWidthShadow)
            {
                GlowHelper.RenderWithShadow(this.Handle, position, _dock, lineWidth, isActive);
            }
            if (_mode == EdgeMode.Border)
            {
                GlowHelper.RenderOnlyLine(this.Handle, position, _dock, lineWidth, isActive);
            }
            if (_mode == EdgeMode.BorderResize)
            {
                GlowHelper.RenderWithResize(this.Handle, position, _dock, lineWidth, isActive);
            }
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