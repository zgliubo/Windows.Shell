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
using Dock = Windows.Shell.ResizeBorderWindow.Dock;
using Microsoft.Win32;
using Windows.Win32.System.Registry;

namespace Windows.Shell
{
    static class GlowHelper
    {
        private static readonly GlowBitmap[] activeGlowBitmaps = new GlowBitmap[10];
        private static readonly GlowBitmap[] inactiveGlowBitmaps = new GlowBitmap[10];

        private static readonly Color activeColor = Color.FromArgb(112, 112, 112);
        private static readonly Color inactiveColor = Color.FromArgb(170, 170, 170);

        public static bool UseDwmColor { get; private set; }

        static GlowHelper()
        {
            RegNotifyChangeKeyValue();
        }

        public static void Render(HWND hwnd, RECT rect, Dock orientation, int lineWidth, bool onlyLine, bool isActive)
        {
            if (!onlyLine)
            {
                Render(hwnd, rect, orientation, lineWidth, isActive);
                return;
            }

            using var ctx = new GlowDrawingContext(rect.Width, rect.Height);
            if (ctx.IsInitialized)
            {
                var width = 0;
                var height = 0;

                switch (orientation)
                {
                    case Dock.Left:
                    case Dock.Right:
                        width = lineWidth;
                        height = ctx.Height;
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        width = ctx.Width;
                        height = lineWidth;
                        break;
                }

                var img = GetOrCreateBitmap(ctx, GlowBitmapPart.Pixel, isActive);
                PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
                PInvoke.AlphaBlend(ctx.WindowDC, 0, 0, width, height, ctx.BackgroundDC, 0, 0, img.Width, img.Height, ctx.Blend);

                var pptDest = new Point(rect.left, rect.top);
                var psize = new SIZE(rect.Width, rect.Height);
                var pptSrc = new Point(0, 0);
                PInvoke.UpdateLayeredWindow(hwnd, ctx.ScreenDC, pptDest, psize, ctx.WindowDC, pptSrc, (COLORREF)0u, ctx.Blend, UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);
            }
        }

        private static void Render(HWND hwnd, RECT rect, Dock orientation, int lineWidth,  bool isActive)
        {
            using var ctx = new GlowDrawingContext(rect.Width, rect.Height);
            if (ctx.IsInitialized)
            {
                var width = 0;
                var height = 0;
                var left = 0;
                var top = 0;

                switch (orientation)
                {
                    case Dock.Left:
                    case Dock.Right:
                        width = lineWidth;
                        height = ctx.Height;
                        left = orientation == Dock.Right ? 0 : ctx.Width - lineWidth;
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        var diff = ctx.Height - lineWidth;
                        width = ctx.Width - 2 * diff;
                        height = lineWidth;
                        top = orientation == Dock.Bottom ? 0 : ctx.Height - lineWidth;
                        left = diff;
                        break;
                }

                var img = GetOrCreateBitmap(ctx, GlowBitmapPart.Pixel, isActive);
                PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
                PInvoke.AlphaBlend(ctx.WindowDC, left, top, width, height, ctx.BackgroundDC, 0, 0, img.Width, img.Height, ctx.Blend);

                switch (orientation)
                {
                    case Dock.Left:
                    case Dock.Right:
                        width = ctx.Width - lineWidth;
                        height = ctx.Height;
                        left = orientation == Dock.Left ? 0 : lineWidth;
                        top = 0;
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        width = ctx.Width;
                        height = ctx.Height - lineWidth;
                        left = 0;
                        top = orientation == Dock.Top ? 0 : lineWidth;
                        break;
                }

                img = GetOrCreateBitmap(ctx, GlowBitmapPart.PixelEmpty, isActive);
                PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
                PInvoke.AlphaBlend(ctx.WindowDC, left, top, width, height, ctx.BackgroundDC, 0, 0, img.Width, img.Height, ctx.Blend);

                var pptDest = new Point(rect.left, rect.top);
                var psize = new SIZE(rect.Width, rect.Height);
                var pptSrc = new Point(0, 0);
                PInvoke.UpdateLayeredWindow(hwnd, ctx.ScreenDC, pptDest, psize, ctx.WindowDC, pptSrc, (COLORREF)0u, ctx.Blend, UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);
            }
        }

        public static void Render(HWND hwnd, RECT rect, Dock orientation, bool isActive)
        {
            using var glowDrawingContext = new GlowDrawingContext(rect.Width, rect.Height);
            if (glowDrawingContext.IsInitialized)
            {
                switch (orientation)
                {
                    case Dock.Left:
                        DrawLeft(glowDrawingContext, isActive);
                        break;
                    case Dock.Top:
                        DrawTop(glowDrawingContext, isActive);
                        break;
                    case Dock.Right:
                        DrawRight(glowDrawingContext, isActive);
                        break;
                    case Dock.Bottom:
                        DrawBottom(glowDrawingContext, isActive);
                        break;
                }

                var pptDest = new Point(rect.left, rect.top);
                var psize = new SIZE(rect.Width, rect.Height);
                var pptSrc = new Point(0, 0);
                PInvoke.UpdateLayeredWindow(hwnd, glowDrawingContext.ScreenDC, pptDest, psize, glowDrawingContext.WindowDC, pptSrc, (COLORREF)0u, glowDrawingContext.Blend, UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);
            }
        }

        private static void DrawLeft(GlowDrawingContext ctx, bool isActive)
        {
            var ox = 0;
            var ow = 9;
            if (ctx.Width < 9)
            {
                ox = 9 - ctx.Width;
                ow = ctx.Width;
            }

            var img = GetOrCreateBitmap(ctx, GlowBitmapPart.Left, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, 0, 0, ctx.Width, ctx.Height, ctx.BackgroundDC, ox, 0, ow, img.Height, ctx.Blend);
        }

        private static void DrawTop(GlowDrawingContext ctx, bool isActive)
        {
            var size = ctx.Height;
            var oy = 0;
            var oh = 9;
            if (ctx.Height < 9)
            {
                oy = 9 - ctx.Height;
                oh = ctx.Height;
            }

            var img = GetOrCreateBitmap(ctx, GlowBitmapPart.Top, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, size, 0, ctx.Width - 2 * size, ctx.Height, ctx.BackgroundDC, 0, oy, img.Width, oh, ctx.Blend);

            img = GetOrCreateBitmap(ctx, GlowBitmapPart.LeftTop, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, 0, 0, size, size, ctx.BackgroundDC, oy, oy, oh, oh, ctx.Blend);

            img = GetOrCreateBitmap(ctx, GlowBitmapPart.RightTop, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, ctx.Width - size, 0, size, size, ctx.BackgroundDC, 0, oy, oh, oh, ctx.Blend);
        }

        private static void DrawRight(GlowDrawingContext ctx, bool isActive)
        {
            //var ox = 0;
            var ow = 9;
            if (ctx.Width < 9)
            {
                //ox = 9 - ctx.Width;
                ow = ctx.Width;
            }

            var img = GetOrCreateBitmap(ctx, GlowBitmapPart.Right, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, 0, 0, ctx.Width, ctx.Height, ctx.BackgroundDC, 0, 0, ow, img.Height, ctx.Blend);
        }

        private static void DrawBottom(GlowDrawingContext ctx, bool isActive)
        {
            var size = ctx.Height;
            var oy = 0;
            var oh = 9;
            if (ctx.Height < 9)
            {
                oy = 9 - ctx.Height;
                oh = ctx.Height;
            }

            var img = GetOrCreateBitmap(ctx, GlowBitmapPart.Bottom, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, size, 0, ctx.Width - 2 * size, ctx.Height, ctx.BackgroundDC, 0, 0, img.Width, oh, ctx.Blend);

            img = GetOrCreateBitmap(ctx, GlowBitmapPart.LeftBottom, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, 0, 0, size, size, ctx.BackgroundDC, oy, 0, oh, oh, ctx.Blend);

            img = GetOrCreateBitmap(ctx, GlowBitmapPart.RightBottom, isActive);
            PInvoke.SelectObject(ctx.BackgroundDC, (HGDIOBJ)img.Handle);
            PInvoke.AlphaBlend(ctx.WindowDC, ctx.Width - size, 0, size, size, ctx.BackgroundDC, 0, 0, oh, oh, ctx.Blend);
        }

        private static GlowBitmap GetOrCreateBitmap(GlowDrawingContext drawingContext, GlowBitmapPart bitmapPart, bool isActive)
        {
            GlowBitmap[] array;
            Color color;
            if (isActive)
            {
                array = activeGlowBitmaps;
                color = UseDwmColor ? Window.ColorizationColor : activeColor;
            }
            else
            {
                array = inactiveGlowBitmaps;
                color = inactiveColor;
            }

            if (array[(int)bitmapPart] == null)
            {
                array[(int)bitmapPart] = GlowBitmap.Create(drawingContext, bitmapPart, color);
            }

            return array[(int)bitmapPart];
        }

        public static void Restore()
        {
            for (int i = 0; i < activeGlowBitmaps.Length; i++)
            {
                activeGlowBitmaps[i]?.Dispose();
                activeGlowBitmaps[i] = null!;
            }
        }

        private static void RegNotifyChangeKeyValue()
        {
            var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM", false)!;

            UseDwmColor = IsWindowPrevalenceAccentColor(key);

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

                    if (IsWindowPrevalenceAccentColor(key) != UseDwmColor)
                    {
                        UseDwmColor = !UseDwmColor;
                        Restore();
                    }
                }
            });
        }

        private static bool IsWindowPrevalenceAccentColor(RegistryKey key)
        {
            //个性化/颜色/"标题栏和边框" 
            //win10 1809 之前没有此设置

            if (!OsVersion.IsWindows10_1809OrGreater && OsVersion.IsWindows8OrGreater)
            {
                //win8 - win10.1803 边框颜色为主题色
                return true;
            }

            if (key.GetValueNames().Contains("ColorPrevalence"))
            {
                int? colorPrevalence = (int?)key.GetValue("ColorPrevalence");

                return colorPrevalence == 1;
            }

            return false;
        }

        enum GlowBitmapPart
        {
            Left,
            Top,
            Right,
            Bottom,
            LeftTop,
            RightTop,
            RightBottom,
            LeftBottom,
            Pixel,
            PixelEmpty,
        }

        class GlowBitmap : DisposableObject
        {
            private sealed class CachedBitmapInfo
            {
                public readonly int Width;

                public readonly int Height;

                public readonly byte[] DIBits;

                public CachedBitmapInfo(byte[] diBits, int width, int height)
                {
                    Width = width;
                    Height = height;
                    DIBits = diBits;
                }
            }

            private const int BytesPerPixelBgra32 = 4;

            private readonly IntPtr _hBitmap;

            private readonly IntPtr _pbits;

            private readonly BITMAPINFO _bitmapInfo;

            public IntPtr Handle => _hBitmap;

            public IntPtr DIBits => _pbits;

            public int Width => _bitmapInfo.biWidth;

            public int Height => -_bitmapInfo.biHeight;

            public GlowBitmap(IntPtr hdcScreen, int width, int height)
            {
                _bitmapInfo.biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                _bitmapInfo.biPlanes = 1;
                _bitmapInfo.biBitCount = 32;
                _bitmapInfo.biCompression = 0u;
                _bitmapInfo.biXPelsPerMeter = 0;
                _bitmapInfo.biYPelsPerMeter = 0;
                _bitmapInfo.biWidth = width;
                _bitmapInfo.biHeight = -height;
                _hBitmap = PInvoke.CreateDIBSection(hdcScreen, ref _bitmapInfo, 0u, out _pbits, IntPtr.Zero, 0u);
            }

            protected override void DisposeNativeResources()
            {
                PInvoke.DeleteObject((HGDIOBJ)_hBitmap);
            }

            private static byte PremultiplyAlpha(byte channel, byte alpha)
            {
                return (byte)((double)(channel * alpha) / 255.0);
            }

            public static GlowBitmap Create(GlowDrawingContext drawingContext, GlowBitmapPart bitmapPart, Color color)
            {
                var alphaMask = CreateAlphaMask(bitmapPart);
                var glowBitmap = new GlowBitmap(drawingContext.ScreenDC, alphaMask.Width, alphaMask.Height);
                for (int i = 0; i < alphaMask.DIBits.Length; i += BytesPerPixelBgra32)
                {
                    byte b = alphaMask.DIBits[i + 3];
                    byte val = PremultiplyAlpha(color.R, b);
                    byte val2 = PremultiplyAlpha(color.G, b);
                    byte val3 = PremultiplyAlpha(color.B, b);
                    Marshal.WriteByte(glowBitmap.DIBits, i, val3);
                    Marshal.WriteByte(glowBitmap.DIBits, i + 1, val2);
                    Marshal.WriteByte(glowBitmap.DIBits, i + 2, val);
                    Marshal.WriteByte(glowBitmap.DIBits, i + 3, b);
                }

                return glowBitmap;
            }

            private static CachedBitmapInfo CreateAlphaMask(GlowBitmapPart bitmapPart)
            {
                switch (bitmapPart)
                {
                    case GlowBitmapPart.Left:
                        return new(new byte[] { 131, 131, 131, 2, 131, 131, 131, 5, 131, 131, 131, 8, 131, 131, 131, 11, 131, 131, 131, 19, 131, 131, 131, 30, 131, 131, 131, 46, 131, 131, 131, 65, 0, 0, 0, 254 }, 9, 1);
                    case GlowBitmapPart.Top:
                        return new(new byte[] { 131, 131, 131, 2, 131, 131, 131, 5, 131, 131, 131, 8, 131, 131, 131, 11, 131, 131, 131, 19, 131, 131, 131, 30, 131, 131, 131, 46, 131, 131, 131, 65, 0, 0, 0, 254 }, 1, 9);
                    case GlowBitmapPart.Right:
                        return new(new byte[] { 0, 0, 0, 254, 131, 131, 131, 65, 131, 131, 131, 46, 131, 131, 131, 30, 131, 131, 131, 19, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 2 }, 9, 1);
                    case GlowBitmapPart.Bottom:
                        return new(new byte[] { 0, 0, 0, 254, 131, 131, 131, 65, 131, 131, 131, 46, 131, 131, 131, 30, 131, 131, 131, 19, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 2 }, 1, 9);
                    case GlowBitmapPart.LeftTop:
                        return new(new byte[] { 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 5, 131, 131, 131, 6, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 8, 131, 131, 131, 11, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 13, 131, 131, 131, 18, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 15, 131, 131, 131, 20, 131, 131, 131, 27, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 5, 131, 131, 131, 8, 131, 131, 131, 13, 131, 131, 131, 20, 131, 131, 131, 28, 131, 131, 131, 38, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 4, 131, 131, 131, 6, 131, 131, 131, 11, 131, 131, 131, 18, 131, 131, 131, 27, 131, 131, 131, 38, 0, 0, 0, 255 }, 9, 9);
                    case GlowBitmapPart.RightTop:
                        return new(new byte[] { 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 6, 131, 131, 131, 5, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 18, 131, 131, 131, 13, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 27, 131, 131, 131, 20, 131, 131, 131, 15, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 38, 131, 131, 131, 28, 131, 131, 131, 20, 131, 131, 131, 13, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 0, 0, 0, 255, 131, 131, 131, 38, 131, 131, 131, 27, 131, 131, 131, 18, 131, 131, 131, 11, 131, 131, 131, 6, 131, 131, 131, 4, 131, 131, 131, 2, 131, 131, 131, 1 }, 9, 9);
                    case GlowBitmapPart.RightBottom:
                        return new(new byte[] { 0, 0, 0, 255, 131, 131, 131, 38, 131, 131, 131, 27, 131, 131, 131, 18, 131, 131, 131, 11, 131, 131, 131, 6, 131, 131, 131, 4, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 38, 131, 131, 131, 28, 131, 131, 131, 20, 131, 131, 131, 13, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 27, 131, 131, 131, 20, 131, 131, 131, 15, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 18, 131, 131, 131, 13, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 6, 131, 131, 131, 5, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0 }, 9, 9);
                    case GlowBitmapPart.LeftBottom:
                        return new(new byte[] { 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 4, 131, 131, 131, 6, 131, 131, 131, 11, 131, 131, 131, 18, 131, 131, 131, 27, 131, 131, 131, 38, 0, 0, 0, 255, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 5, 131, 131, 131, 8, 131, 131, 131, 13, 131, 131, 131, 20, 131, 131, 131, 28, 131, 131, 131, 38, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 15, 131, 131, 131, 20, 131, 131, 131, 27, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 13, 131, 131, 131, 18, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 8, 131, 131, 131, 11, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 5, 131, 131, 131, 6, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1 }, 9, 9);
                    case GlowBitmapPart.Pixel:
                        return new(new byte[] { 0, 0, 0, 254 }, 1, 1);
                    case GlowBitmapPart.PixelEmpty:
                        return new(new byte[] { 0, 0, 0, 1 }, 1, 1);
                    default:
                        break;
                }
                return null!;
            }
        }

        class GlowDrawingContext : DisposableObject
        {
            public BLENDFUNCTION Blend;

            private readonly HDC hdcScreen;

            private readonly HDC hdcWindow;

            private readonly GlowBitmap? windowBitmap;

            private readonly HDC hdcBackground;

            public bool IsInitialized
            {
                get
                {
                    if (hdcScreen != IntPtr.Zero && hdcWindow != IntPtr.Zero && hdcBackground != IntPtr.Zero)
                    {
                        return windowBitmap != null;
                    }

                    return false;
                }
            }

            public HDC ScreenDC => hdcScreen;

            public HDC WindowDC => hdcWindow;

            public HDC BackgroundDC => hdcBackground;

            public int Width => windowBitmap?.Width ?? 0;

            public int Height => windowBitmap?.Height ?? 0;

            public GlowDrawingContext(int width, int height)
            {
                hdcScreen = PInvoke.GetDC(HWND.Null);
                if (hdcScreen == 0)
                {
                    return;
                }

                hdcWindow = PInvoke.CreateCompatibleDC(hdcScreen);
                if (!(hdcWindow == 0))
                {
                    hdcBackground = PInvoke.CreateCompatibleDC(hdcScreen);
                    if (!(hdcBackground == 0))
                    {
                        Blend.BlendOp = 0;
                        Blend.BlendFlags = 0;
                        Blend.SourceConstantAlpha = byte.MaxValue;
                        Blend.AlphaFormat = 1;
                        windowBitmap = new GlowBitmap(ScreenDC, width, height);
                        PInvoke.SelectObject(hdcWindow, (HGDIOBJ)windowBitmap.Handle);
                    }
                }
            }

            protected override void DisposeManagedResources()
            {
                windowBitmap?.Dispose();
            }

            protected override void DisposeNativeResources()
            {
                if (hdcScreen != 0)
                {
                    _ = PInvoke.ReleaseDC(HWND.Null, hdcScreen);
                }

                if (hdcWindow != 0)
                {
                    PInvoke.DeleteDC(hdcWindow);
                }

                if (hdcBackground != 0)
                {
                    PInvoke.DeleteDC(hdcBackground);
                }
            }
        }
    }
}
