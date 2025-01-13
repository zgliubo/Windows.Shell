using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Globalization;
using System.Drawing;
using EdgeMode = Windows.Shell.WindowEdge.EdgeMode;

namespace Windows.Shell
{
    class Window: DisposableObject
    {
        private readonly WNDPROC _wndProc;
        private readonly string className;
        private readonly HWND owner;
        private TopDragWindow? topDragWindow;
        private ControlWindow? controlWindow;
        private WindowEdge? windowEdge;
        private bool isActive;
        private bool dwmEnabled;

        public static Color ColorizationColor { get; private set; }

        static Window()
        {
            GetColorizationColor();
            Console.WriteLine(ColorizationColor);
        }

        public Window(HWND? owner = null)
        {
            _wndProc = WndProc;
            className = Guid.NewGuid().ToString();
            RegisterClass(className);
            Handle = CreateWindow(className, owner);
            Init();
        }

        public HWND Handle { get; }

        private ResizeMode _ResizeMode = ResizeMode.CanResize;
        public ResizeMode ResizeMode
        {
            get { return _ResizeMode; }
            set
            {
                _ResizeMode = value;
                CreateResizibility();
            }
        }

        public void Show() 
        {
            PInvoke.ShowWindow(Handle, SHOW_WINDOW_CMD.SW_SHOW);
        }

        public void Hide()
        {
            PInvoke.ShowWindow(Handle, SHOW_WINDOW_CMD.SW_HIDE);
        }

        public void Close()
        {
            Dispose();
        }

        private void Init()
        {
            CreateResizibility();

            PInvoke.DwmIsCompositionEnabled(out var dwmEnabled);
            this.dwmEnabled = dwmEnabled;

            if (OsVersion.IsWindows10_1507OrGreater) //win10及以上
            {
                topDragWindow = new TopDragWindow(default, Handle);
                controlWindow = new ControlWindow(default, Handle);

                if (!OsVersion.IsWindows11_OrGreater) //win10
                {
                    windowEdge = new WindowEdge(default, Handle, EdgeMode.Border);
                }
            }
            else 
            {
                if (OsVersion.IsWindows8OrGreater && !OsVersion.IsWindows10_1507OrGreater)
                {
                    //win8 win8.1
                    windowEdge = new WindowEdge(default, Handle, EdgeMode.BorderResizeWidthShadow);
                }
                else if (dwmEnabled)
                {
                    //win7 dwm启用时
                    windowEdge = new WindowEdge(default, Handle, EdgeMode.BorderResize, EdgeMode.BorderResizeWidthShadow);
                }
                else
                {
                    // win7 dwm禁用时
                    windowEdge = new WindowEdge(default, Handle, EdgeMode.BorderResizeWidthShadow, EdgeMode.BorderResize);
                }

                UpdateWindowPos(Handle);
            }
        }

        private unsafe void RegisterClass(string className)
        {
            var wNDCLASSEXW = default(WNDCLASSEXW);
            wNDCLASSEXW.cbSize = (uint)Marshal.SizeOf(wNDCLASSEXW);
            wNDCLASSEXW.style = WNDCLASS_STYLES.CS_VREDRAW | WNDCLASS_STYLES.CS_HREDRAW;
            wNDCLASSEXW.lpfnWndProc = _wndProc;
            wNDCLASSEXW.cbClsExtra = 0;
            wNDCLASSEXW.cbWndExtra = 0;
            wNDCLASSEXW.hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
            wNDCLASSEXW.hIcon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION);
            wNDCLASSEXW.hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW);
            wNDCLASSEXW.hbrBackground = new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH));
            wNDCLASSEXW.lpszMenuName = (PCWSTR)null;

            fixed (char* c = className)
            {
                wNDCLASSEXW.lpszClassName = c;
                if (PInvoke.RegisterClassEx(wNDCLASSEXW) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        public static unsafe HWND CreateWindow(string className, HWND? owner = null)
        {
            var hwnd = PInvoke.CreateWindowEx(
                 WINDOW_EX_STYLE.WS_EX_APPWINDOW,
                 className,
                 string.Empty,
                 WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
                 PInvoke.CW_USEDEFAULT,
                 PInvoke.CW_USEDEFAULT,
                 PInvoke.CW_USEDEFAULT,
                 PInvoke.CW_USEDEFAULT,
                 owner ?? HWND.Null,
                 new DestroyMenuSafeHandle(HMENU.Null, false),
                 PInvoke.GetModuleHandle((string?)null),
                 null);

            if (hwnd.IsNull)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return hwnd;
        }

        private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            var handled = false;
            var result = default(LRESULT);

            switch (msg)
            {
                case PInvoke.WM_SIZE:
                    WmSize(hwnd, wParam, lParam);
                    break;
                case PInvoke.WM_SETCURSOR:
                    result = WmSetCursor(hwnd, wParam, lParam, ref handled);
                    break;
                case PInvoke.WM_WINDOWPOSCHANGED:
                    WmWindowPosChanged(lParam);
                    break;
                case PInvoke.WM_NCCALCSIZE:
                    return WmNcCalcSize(hwnd, lParam, ref handled);
                case PInvoke.WM_NCHITTEST:
                    result = WmNcHitTest(hwnd, lParam, ref handled);
                    break;
                case PInvoke.WM_NCACTIVATE:
                    result = WmNcActivate(hwnd, wParam, ref handled);
                    break;
                case PInvoke.WM_DWMCOMPOSITIONCHANGED:
                    WmDwmCompositionChanged(hwnd);
                    break;
                case PInvoke.WM_ACTIVATE:
                    WmActivate(hwnd, wParam);
                    break;
                case PInvoke.WM_DWMCOLORIZATIONCOLORCHANGED:
                    WmDwmColorizationColorChanged();
                    break;
            }

            if (handled)
            {
                return result;
            }

            return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private void WmSize(HWND hwnd, WPARAM wParam, LPARAM lParam)
        {
            var rect = new RECT(0, 0, PInvoke.PARAM.LOWORD(lParam), PInvoke.PARAM.HIWORD(lParam));

            if (controlWindow != null || topDragWindow != null)
            {
                var dpi = WindowHelper.GetDpiForWindow(hwnd);
                var size = WindowHelper.GetWindowBorderSize(dpi);
                topDragWindow?.UpdatePosition(new RECT(0, 0, rect.Width, size.Height));

                var height = (int)Math.Round((double)ControlWindow.ButtonHeight * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI);
                var count = this.ResizeMode == ResizeMode.NoResize ? 1 : 3;
                var width = (int)Math.Round((double)ControlWindow.ButtonWidth * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI) * count;
                controlWindow?.UpdatePosition(new RECT(rect.Width - width, 0, rect.Width, height));
            }

            if (wParam == PInvoke.SIZE_MAXIMIZED)
            {
                topDragWindow?.Hide();
                windowEdge?.Hide();
            }
            else if (wParam == PInvoke.SIZE_RESTORED)
            {
                topDragWindow?.Show();
                windowEdge?.Show();
            }

            if (!dwmEnabled)
            {
                if (wParam == PInvoke.SIZE_MAXIMIZED)
                {
                    _ = PInvoke.SetWindowRgn(hwnd, HRGN.Null, true);
                }
                else if (wParam == PInvoke.SIZE_RESTORED)
                {
                    var hRgn = PInvoke.CreateRectRgnIndirect(rect);
                    _ = PInvoke.SetWindowRgn(hwnd, hRgn, true);
                }
            }
        }

        private LRESULT WmSetCursor(HWND hwnd, WPARAM wParam, LPARAM lParam, ref bool handled)
        {
            //对于自定义边框, 子窗口为模态框时, 激活闪烁 (需要配合WM_NCACTIVATE)
            if (windowEdge != null)
            {
                if (PInvoke.PARAM.SignedLOWORD(lParam) == PInvoke.HTERROR && PInvoke.PARAM.SignedHIWORD(lParam) == PInvoke.WM_LBUTTONDOWN)
                {
                    handled = true;
                    PInvoke.SendMessage(hwnd, PInvoke.WM_SETCURSOR, wParam, PInvoke.PARAM.FromLowHighUnsigned(PInvoke.HTERROR, (int)PInvoke.WM_RBUTTONDOWN));
                }
            }
            return new LRESULT(0);
        }

        private void WmWindowPosChanged(LPARAM lParam)
        {
            if (windowEdge != null)
            {
                var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                var sizeChanged = (windowPos.flags & SET_WINDOW_POS_FLAGS.SWP_NOSIZE) == 0;
                var posChanged = (windowPos.flags & SET_WINDOW_POS_FLAGS.SWP_NOMOVE) == 0;

                if (sizeChanged || posChanged)
                {
                    PInvoke.GetClientRect(Handle, out var rect);
                    windowEdge.UpdatePosition(rect);
                }
            }
        }

        private LRESULT WmNcCalcSize(HWND hwnd, LPARAM lParam, ref bool handled)
        {
            var placement = default(WINDOWPLACEMENT);
            placement.length = (uint)Marshal.SizeOf(placement);
            PInvoke.GetWindowPlacement(hwnd, ref placement);
            if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED)
            {
                var monitorInfo = default(MONITORINFO);
                monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
                PInvoke.GetMonitorInfo(PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST), ref monitorInfo);
                var workArea = monitorInfo.rcWork;

                var pData = default(APPBARDATA);
                pData.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));

                if (PInvoke.SHAppBarMessage(PInvoke.ABM_GETSTATE, ref pData) != 0
                    && PInvoke.SHAppBarMessage(PInvoke.ABM_GETSTATE, ref pData) != 0)
                {
                    switch (pData.uEdge)
                    {
                        case PInvoke.ABE_LEFT:
                            workArea.left++;
                            break;
                        case PInvoke.ABE_TOP:
                            workArea.top++;
                            break;
                        case PInvoke.ABE_RIGHT:
                            workArea.right--;
                            break;
                        case PInvoke.ABE_BOTTOM:
                            workArea.bottom--;
                            break;
                    }
                }

                Marshal.StructureToPtr(workArea, lParam, true);
            }
            else
            {
                var dpi = WindowHelper.GetDpiForWindow(hwnd);
                var bw = (int)Math.Round(1.0 * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI);
                var rect = Marshal.PtrToStructure<RECT>(lParam);
              
                if (OsVersion.IsWindows10_1507OrGreater)
                {
                    var size = WindowHelper.GetWindowBorderSize(dpi);
                    rect.left += size.Width;
                    rect.right -= size.Width;
                    rect.bottom -= size.Height;
                    if (OsVersion.IsWindows11_OrGreater)
                    {
                        rect.top += bw;
                    }
                }
                else if(dwmEnabled)
                {
                    rect.left += bw;
                    rect.right -= bw;
                    rect.bottom -= bw;
                }

                Marshal.StructureToPtr(rect, lParam, true);
            }

            handled = true;
            return new LRESULT(0);
        }

        private LRESULT WmNcHitTest(HWND hwnd, LPARAM lParam, ref bool handled)
        {
            var _pos = PInvoke.PARAM.ToPoint(lParam);
            var pos = WindowHelper.PointToClient(hwnd, _pos);
            var dpi = WindowHelper.GetDpiForWindow(hwnd);

            if (windowEdge == null)
            {
                var size = WindowHelper.GetWindowBorderSize(dpi);
                if (pos.Y < size.Height)
                {
                    if (pos.X < 0)
                    {
                        handled = true;
                        return new LRESULT((nint)PInvoke.HTTOPLEFT);
                    }
                    PInvoke.GetClientRect(hwnd, out var rect);
                    if (pos.X > rect.right)
                    {
                        handled = true;
                        return new LRESULT((nint)PInvoke.HTTOPRIGHT);
                    }
                }
            }

            if (pos.Y < 32 * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI)
            {
                if (pos.X < 0)
                {
                    handled = false;
                    return new LRESULT((nint)PInvoke.HTTOPLEFT);
                }
                PInvoke.GetClientRect(hwnd, out var rect);
                if (pos.X > rect.right)
                {
                    handled = false;
                    return new LRESULT(0);
                }
                handled = true;
                return new LRESULT(0);
            }

            return new LRESULT(0);
        }

        private void WmActivate(HWND hwnd, WPARAM wParam)
        {
            isActive = wParam != 0;

            if (windowEdge != null)
            {
                PInvoke.GetClientRect(hwnd, out var rect);
                windowEdge.UpdatePositionWidthActive(rect, isActive);
            }
        }

        private void WmDwmCompositionChanged(HWND hwnd)
        {
            PInvoke.DwmIsCompositionEnabled(out var dwmEnabled);
            this.dwmEnabled = dwmEnabled;

            if (windowEdge != null)
            {
                PInvoke.GetClientRect(hwnd, out var rect);
                windowEdge.UpdateToNextMode(rect, hwnd, isActive);

                if (dwmEnabled)
                {
                    _ = PInvoke.SetWindowRgn(hwnd, HRGN.Null, true);
                }
                else
                {
                    var hRgn = PInvoke.CreateRectRgnIndirect(rect);
                    _ = PInvoke.SetWindowRgn(hwnd, hRgn, true);
                }
            }
        }

        private LRESULT WmNcActivate(HWND hwnd, WPARAM wParam, ref bool handled)
        {
            if (windowEdge != null)
            {
                PInvoke.GetClientRect(hwnd, out var rect);
                windowEdge.UpdatePositionWidthActive(rect, wParam != 0);
            }

            if (!dwmEnabled)
            {
                handled = true;
                return PInvoke.DefWindowProc(hwnd, PInvoke.WM_NCACTIVATE, wParam, -1);
            }

            return new LRESULT(0);
        }

        private void WmDwmColorizationColorChanged()
        {
            GetColorizationColor();
            if (windowEdge != null && GlowHelper.UseDwmColor)
            {
                GlowHelper.Restore();
            }
        }

        private void CreateResizibility()
        {
            if (Handle.IsNull)
            {
                return;
            }

            var style = (uint)PInvoke.GetWindowLong(this.Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            style &= ~(uint)(WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_MAXIMIZEBOX | WINDOW_STYLE.WS_MINIMIZEBOX);

            switch (ResizeMode)
            {
                case ResizeMode.NoResize:
                    break;
                case ResizeMode.CanMinimize:
                    style |= (uint)WINDOW_STYLE.WS_MINIMIZEBOX;
                    break;
                case ResizeMode.CanResize:
                    style |= (uint)(WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_MAXIMIZEBOX | WINDOW_STYLE.WS_MINIMIZEBOX);
                    break;
            }

            PInvoke.SetWindowLong(this.Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)style);
        }

        private static void ExtendGlassFrame(HWND hwnd)
        {
            var margin = new Win32.UI.Controls.MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 };
            PInvoke.DwmExtendFrameIntoClientArea(hwnd, margin);
            var flags = SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            PInvoke.SetWindowPos(hwnd, HWND.Null, 0, 0, 0, 0, flags);
        }

        private static void UpdateWindowPos(HWND hwnd)
        {
            var flags = SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            PInvoke.SetWindowPos(hwnd, HWND.Null, 0, 0, 0, 0, flags);
        }

        public static void GetColorizationColor()
        {
            try
            {
                PInvoke.DwmGetColorizationColor(out var color, out _);
                ColorizationColor = Color.FromArgb(
                    (byte)((color >> 24) & 0xFF),
                    (byte)((color >> 16) & 0xFF),
                    (byte)((color >> 8) & 0xFF),
                    (byte)(color & 0xFF));
            }
            catch { }
        }

        override protected void DisposeManagedResources()
        {
            windowEdge?.Close();
            windowEdge = null;
            controlWindow?.Close();
            controlWindow = null;
            topDragWindow?.Close();
            topDragWindow = null;
        }

        protected override void DisposeNativeResources()
        {
            PInvoke.DestroyWindow(Handle);
            PInvoke.UnregisterClass(this.className, PInvoke.GetModuleHandle((string?)null));
        }
    }

    public enum ResizeMode
    {
        /// <summary>
        /// 用户无法调整窗口的大小。 不显示“最大化”和“最小化”框。
        /// </summary>
        NoResize,
        /// <summary>
        /// 用户只能最小化窗口，并从任务栏还原它。 “最小化”和“最大化”框均显示，但仅启用“最小化”框。
        /// </summary>
        CanMinimize,
        /// <summary>
        /// 用户可以使用“最小化”和“最大化”框以及窗口周围的可拖动轮廓来调整窗口大小。 显示并启用“最小化”和“最大化”框。 (默认) 。
        /// </summary>
        CanResize
    }
}