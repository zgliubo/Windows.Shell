using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Windows.Shell
{
    internal class Webview2 : DisposableObject
    {
        public Microsoft.Web.WebView2.WinForms.WebView2? WinFormsWebView2 { get; set; }

        private readonly HWND parentWindow;

        private CoreWebView2Controller? coreWebView2Controller;

        private bool isBrowserHitTransparent;

        private bool isInitialized;

        private Task? initTask;

        private readonly NativeWindowBridge nativeWindowBridge;

        public Webview2(HWND parentWindow)
        {
            this.parentWindow = parentWindow;
            nativeWindowBridge = new NativeWindowBridge(parentWindow);
        }

        private Uri _source = null!;
        public Uri Source
        {
            get
            {
                return _source;
            }
            set
            {
                if (value == null)
                {
                    if (!(_source == null))
                    {
                        throw new NotImplementedException("Setting Source to null is not implemented yet.");
                    }

                    return;
                }

                if (!value.IsAbsoluteUri)
                {
                    throw new ArgumentException("Only absolute URI is allowed", "Source");
                }

                if (isInitialized && _source != null && _source.AbsoluteUri == value.AbsoluteUri)
                {
                    return;
                }

                _source = value;
                if (!isInitialized)
                {
                    _ = EnsureCoreWebView2Async();
                }
                else
                {
                    CoreWebView2?.Navigate(value.AbsoluteUri);
                }
            }
        }

        public CoreWebView2? CoreWebView2
        {
            get
            {
                try
                {
                    return coreWebView2Controller?.CoreWebView2;
                }
                catch (Exception innerException)
                {
                    Console.WriteLine(innerException);
                    return null;
                }
            }
        }

        public void SetSize(RECT rect)
        {
            if (coreWebView2Controller != null)
                coreWebView2Controller.Bounds = rect;
        }

        public void SetFocus()
        {
            coreWebView2Controller?.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
        }

        private Task EnsureCoreWebView2Async()
        {
            if (initTask == null || initTask.IsFaulted)
            {
                initTask = InitCoreWebView2Async();
            }

            return initTask;
        }

        private async Task InitCoreWebView2Async()
        {
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync();
                coreWebView2Controller = await environment.CreateCoreWebView2ControllerAsync(parentWindow);
                coreWebView2Controller.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
                PInvoke.GetClientRect(parentWindow, out var rect);
                coreWebView2Controller.Bounds = rect;
                coreWebView2Controller.IsVisible = true;
                isBrowserHitTransparent = GetIsBrowserHitTransparent(coreWebView2Controller);

                bool num = _source != null;
                if (_source == null)
                {
                    _source = new Uri(CoreWebView2!.Source);
                }

                isInitialized = true;
                this.CoreWebView2!.AddHostObjectToScript("nativeWindow", nativeWindowBridge);

                if (num)
                {
                    CoreWebView2!.Navigate(_source.AbsoluteUri);
                }
            }
            catch (Exception ex3)
            {
                Console.WriteLine(ex3);
                throw;
            }
        }

        private static bool GetIsBrowserHitTransparent(CoreWebView2Controller controller)
        {
            try
            {
                Type type = typeof(CoreWebView2Controller);
                var prop = type.GetProperty("IsBrowserHitTransparent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return prop?.GetValue(controller) as bool? ?? false;
            }
            catch (NotImplementedException)
            {
            }

            return false;
        }
    }

#pragma warning disable CS0618 // 类型或成员已过时
    [ClassInterface(ClassInterfaceType.AutoDual)]
#pragma warning restore CS0618 // 类型或成员已过时
    [ComVisible(true)]
    public class NativeWindowBridge
    {
        readonly HWND windowHandle;

        public NativeWindowBridge(nint target)
        {
            this.windowHandle = (HWND)target;
        }

        public void HitTestCaption(bool doubleClick)
        {
            PInvoke.ReleaseCapture();
            var msg = doubleClick ? PInvoke.WM_NCLBUTTONDBLCLK : PInvoke.WM_NCLBUTTONDOWN;
            PInvoke.PostMessage(windowHandle, msg, PInvoke.HTCAPTION, 0);
        }
    }
}
