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
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Windows.Shell
{
    class NonClientWindow: DisposableObject
    {
        const string CLASS_NAME = "NONCLIENT_WINDOW_CLASS";

        static NonClientWindow()
        {
            RegisterClass();
        }

        private readonly WNDPROC wndProc;
        private readonly bool asChild;
        private readonly bool transparent;
        private readonly bool layered;
        protected HWND Handle { get; }
        protected HWND ParentHandle { get; }

        public NonClientWindow(RECT position, HWND parent, WINDOW_EX_STYLE dwExStyle, WINDOW_STYLE dwStyle)
            : this(position, parent, dwExStyle, dwStyle, false) { }

        public NonClientWindow(RECT position, HWND parent, WINDOW_EX_STYLE dwExStyle, WINDOW_STYLE dwStyle, bool setLayered)
        {
            wndProc = WndProc;
            ParentHandle = parent;
            Handle = CreateWindow(position, parent, dwExStyle, dwStyle);

            if (setLayered)
            {
                PInvoke.SetLayeredWindowAttributes(Handle, (COLORREF)0, 255, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
            }
            
            SubclassWndProc();
        }

        private static unsafe void RegisterClass()
        {
            var wNDCLASSEXW = default(WNDCLASSEXW);
            wNDCLASSEXW.cbSize = (uint)Marshal.SizeOf(wNDCLASSEXW);
            wNDCLASSEXW.style = WNDCLASS_STYLES.CS_VREDRAW | WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_DBLCLKS;
            wNDCLASSEXW.lpfnWndProc = PInvoke.DefWindowProc;
            wNDCLASSEXW.cbClsExtra = 0;
            wNDCLASSEXW.cbWndExtra = 0;
            wNDCLASSEXW.hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
            wNDCLASSEXW.hIcon = HICON.Null;
            wNDCLASSEXW.hCursor = HCURSOR.Null;
            wNDCLASSEXW.hbrBackground = HBRUSH.Null;
            wNDCLASSEXW.lpszMenuName = (PCWSTR)null;

            fixed (char* c = CLASS_NAME)
            {
                wNDCLASSEXW.lpszClassName = c;
                if (PInvoke.RegisterClassEx(wNDCLASSEXW) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        private unsafe HWND CreateWindow(RECT position, HWND parent, WINDOW_EX_STYLE dwExStyle, WINDOW_STYLE dwStyle)
        {
            dwStyle |= WINDOW_STYLE.WS_VISIBLE;

            var hwnd = PInvoke.CreateWindowEx(
                 dwExStyle,
                 CLASS_NAME,
                 string.Empty,
                 dwStyle,
                 position.left,
                 position.top,
                 position.Width,
                 position.Height,
                 parent,
                 new DestroyMenuSafeHandle(HMENU.Null, false),
                 PInvoke.GetModuleHandle((string?)null),
                 null);

            if (hwnd == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return hwnd;
        }

        private void SubclassWndProc()
        {
            PInvoke.SetWindowLong(Handle, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(wndProc));
        }

        protected virtual LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        public void Show()
        {
            PInvoke.ShowWindow(Handle, SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
        }

        public virtual void UpdatePosition(RECT position)
        {
            PInvoke.MoveWindow(Handle, position.left, position.top, position.Width, position.Height, true);
        }

        public void Hide()
        {
            PInvoke.ShowWindow(Handle, SHOW_WINDOW_CMD.SW_HIDE);
        }

        public void Close()
        {
            Dispose();
        }

        protected override void DisposeNativeResources()
        {
            PInvoke.DestroyWindow(Handle);
        }
    }
}
