using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Globalization;

namespace Windows.Shell
{
    class Window
    {
        private readonly WNDPROC _wndProc;
        private readonly string _className;
        public HWND Handle { get; private set; }
        private TopDragWindow? topDragWindow;
        private ControlWindow? controlWindow;
        private List<ResizeWindow>? resizeWindows;
        private bool isActive;
        private bool dwmEnabled;

        public Window()
        {
            _wndProc = WndProc;
            _className = CreateClassName();
        }

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

        private static string CreateClassName()
        {
            string appName;
            if (null != AppDomain.CurrentDomain.FriendlyName && 128 <= AppDomain.CurrentDomain.FriendlyName.Length)
                appName = AppDomain.CurrentDomain.FriendlyName[..128];
            else
                appName = AppDomain.CurrentDomain.FriendlyName!;

            string threadName;
            if (null != Thread.CurrentThread.Name && 64 <= Thread.CurrentThread.Name.Length)
                threadName = Thread.CurrentThread.Name[..64];
            else
                threadName = Thread.CurrentThread.Name!;

            string randomName = Guid.NewGuid().ToString();
            string className = string.Format(CultureInfo.InvariantCulture, "HwndWrapper[{0};{1};{2}]", appName, threadName, randomName);

            return className;
        }

        private unsafe void RegisterClass()
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

            fixed (char* c = _className)
            {
                wNDCLASSEXW.lpszClassName = c;
                if (PInvoke.RegisterClassEx(wNDCLASSEXW) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        public unsafe bool CreateWindow(HWND? owner = null)
        {
            RegisterClass();

            var hwnd = PInvoke.CreateWindowEx(
                 WINDOW_EX_STYLE.WS_EX_APPWINDOW,
                 _className,
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
            this.Handle = hwnd;

            CreateResizibility();

            PInvoke.DwmIsCompositionEnabled(out var dwmEnabled);
            this.dwmEnabled = dwmEnabled;

            if (OsVersion.IsWindows10_1507OrGreater)
            {
                topDragWindow = new TopDragWindow(default, hwnd);
                controlWindow = new ControlWindow(default, hwnd);
            }
            else
            {
                resizeWindows = ResizeWindow.CreateWindows(default, hwnd);
            }

            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);

            if (this.ResizeMode != ResizeMode.NoResize)
            {
                topDragWindow?.Show();
                resizeWindows?.ForEach(x => x.Show());
            }

            controlWindow?.Show();

            return true;
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

            if (controlWindow != null)
            {
                var dpi = WindowHelper.GetDpiForWindow(hwnd);
                var size = WindowHelper.GetWindowBorderSize(dpi);
                topDragWindow?.UpdatePosition(new RECT(0, 0, rect.Width, size.Height));

                var height = (int)Math.Round((double)ControlWindow.ButtonHeight * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI);
                var count = this.ResizeMode == ResizeMode.NoResize ? 1 : 3;
                var width = (int)Math.Round((double)ControlWindow.ButtonWidth * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI) * count;
                controlWindow?.UpdatePosition(new RECT(rect.Width - width, 0, rect.Width, height));
            }

            if (!dwmEnabled)
            {
                if (wParam == PInvoke.SIZE_MAXIMIZED)
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

        private LRESULT WmSetCursor(HWND hwnd, WPARAM wParam, LPARAM lParam, ref bool handled)
        {
            //对于DWM不可用时, 模态框闪烁
            if (resizeWindows != null)
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
            if (resizeWindows != null)
            {
                var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                var sizeChanged = (windowPos.flags & SET_WINDOW_POS_FLAGS.SWP_NOSIZE) == 0;
                var posChanged = (windowPos.flags & SET_WINDOW_POS_FLAGS.SWP_NOMOVE) == 0;
                var show = (windowPos.flags & SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW) == SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW;

                var rect = default(RECT);

                if (sizeChanged || posChanged)
                {
                    PInvoke.GetClientRect(this.Handle, out rect);
                    resizeWindows.ForEach(x => x.UpdatePosition(rect));
                }
            }
        }

        private static LRESULT WmNcCalcSize(HWND hwnd, LPARAM lParam, ref bool handled)
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
            else if (OsVersion.IsWindows10_1507OrGreater)
            {
                var size = WindowHelper.GetWindowBorderSize(WindowHelper.GetDpiForWindow(hwnd));
                var rect = Marshal.PtrToStructure<RECT>(lParam);
                rect.left += size.Width;
                rect.right -= size.Width;
                rect.bottom -= size.Height;
                Marshal.StructureToPtr(rect, lParam, true);
            }

            handled = true;
            return new LRESULT(0);
        }

        private static LRESULT WmNcHitTest(HWND hwnd, LPARAM lParam, ref bool handled)
        {
            var _pos = PInvoke.PARAM.ToPoint(lParam);
            var pos = WindowHelper.PointToClient(hwnd, _pos);
            var dpi = WindowHelper.GetDpiForWindow(hwnd);

            if (pos.Y < 32 * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI)
            {
                handled = true;
                return new LRESULT((nint)PInvoke.HTCAPTION);
            }

            if (OsVersion.IsWindows10_1507OrGreater)
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

            return new LRESULT(0);
        }

        private void WmActivate(HWND hwnd, WPARAM wParam)
        {
            isActive = wParam != 0;

            if (resizeWindows != null)
            {
                PInvoke.GetClientRect(hwnd, out var rect);
                resizeWindows.ForEach(x => x.UpdateGlow(rect, isActive));
            }
        }

        private void WmDwmCompositionChanged(HWND hwnd)
        {
            PInvoke.DwmIsCompositionEnabled(out var dwmEnabled);
            this.dwmEnabled = dwmEnabled;

            if (resizeWindows != null)
            {
                PInvoke.GetClientRect(hwnd, out var rect);
                ResizeWindow.OnDwmChanged(this.resizeWindows, rect, hwnd, dwmEnabled, isActive);
            }
        }

        private LRESULT WmNcActivate(HWND hwnd, WPARAM wParam, ref bool handled)
        {
            if (resizeWindows != null)
            {
                PInvoke.GetClientRect(hwnd, out var rect);
                resizeWindows.ForEach(x => x.UpdateGlow(rect, wParam != 0));

                handled = true;
                return PInvoke.DefWindowProc(hwnd, PInvoke.WM_NCACTIVATE, wParam, -1);
            }

            return new LRESULT(0);
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
