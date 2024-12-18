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
using System.Reflection.Metadata;

namespace Windows.Shell
{
    static class GlowHelper
    {
        private static readonly GlowBitmap[] activeGlowBitmaps = new GlowBitmap[8];
        private static readonly GlowBitmap[] inactiveGlowBitmaps = new GlowBitmap[8];

        public static void RenderLayeredWindow(HWND hwnd, RECT rect, ResizeWindow.Dock orientation, bool isActive)
        {
            using var glowDrawingContext = new GlowDrawingContext(rect.Width, rect.Height);
            if (glowDrawingContext.IsInitialized)
            {
                switch (orientation)
                {
                    case ResizeWindow.Dock.Left:
                        DrawLeft(glowDrawingContext, isActive);
                        break;
                    case ResizeWindow.Dock.Top:
                        DrawTop(glowDrawingContext, isActive);
                        break;
                    case ResizeWindow.Dock.Right:
                        DrawRight(glowDrawingContext, isActive);
                        break;
                    case ResizeWindow.Dock.Bottom:
                        DrawBottom(glowDrawingContext, isActive);
                        break;
                }

                Point point = default;
                point.X = rect.left;
                point.Y = rect.top;
                Point pptDest = point;
                SIZE win32SIZE = default;
                win32SIZE.cx = rect.Width;
                win32SIZE.cy = rect.Height;
                SIZE psize = win32SIZE;
                point = default;
                point.X = 0;
                point.Y = 0;
                Point pptSrc = point;
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
                color = Color.FromArgb(75, 75, 75);
            }
            else
            {
                array = inactiveGlowBitmaps;
                color = Color.FromArgb(180, 180, 180);
            }

            if (array[(int)bitmapPart] == null)
            {
                array[(int)bitmapPart] = GlowBitmap.Create(drawingContext, bitmapPart, color);
            }

            return array[(int)bitmapPart];
        }
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
        LeftBottom
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

        private static readonly CachedBitmapInfo[] _transparencyMasks;

        private readonly IntPtr _hBitmap;

        private readonly IntPtr _pbits;

        private readonly BITMAPINFO _bitmapInfo;

        public IntPtr Handle => _hBitmap;

        public IntPtr DIBits => _pbits;

        public int Width => _bitmapInfo.biWidth;

        public int Height => -_bitmapInfo.biHeight;

        static GlowBitmap()
        {
            /*
            left: 131,131,131,2,131,131,131,5,131,131,131,8,131,131,131,11,131,131,131,19,131,131,131,30,131,131,131,46,131,131,131,65,0,0,0,254
            top: 131,131,131,2,131,131,131,5,131,131,131,8,131,131,131,11,131,131,131,19,131,131,131,30,131,131,131,46,131,131,131,65,0,0,0,254
            right: 0,0,0,254,131,131,131,65,131,131,131,46,131,131,131,30,131,131,131,19,131,131,131,11,131,131,131,8,131,131,131,5,131,131,131,2
            bottom: 0,0,0,254,131,131,131,65,131,131,131,46,131,131,131,30,131,131,131,19,131,131,131,11,131,131,131,8,131,131,131,5,131,131,131,2
            cornertopleft: 131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,2,131,131,131,0,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,3,131,131,131,3,131,131,131,4,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,5,131,131,131,6,131,131,131,0,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,7,131,131,131,8,131,131,131,11,131,131,131,0,131,131,131,1,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,7,131,131,131,10,131,131,131,13,131,131,131,18,131,131,131,1,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,7,131,131,131,10,131,131,131,15,131,131,131,20,131,131,131,27,131,131,131,1,131,131,131,2,131,131,131,3,131,131,131,5,131,131,131,8,131,131,131,13,131,131,131,20,131,131,131,28,131,131,131,38,131,131,131,1,131,131,131,2,131,131,131,4,131,131,131,6,131,131,131,11,131,131,131,18,131,131,131,27,131,131,131,38,0,0,0,255
            cornertopright: 131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,2,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,4,131,131,131,3,131,131,131,3,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,0,131,131,131,6,131,131,131,5,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,11,131,131,131,8,131,131,131,7,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,0,131,131,131,18,131,131,131,13,131,131,131,10,131,131,131,7,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,1,131,131,131,0,131,131,131,27,131,131,131,20,131,131,131,15,131,131,131,10,131,131,131,7,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,1,131,131,131,38,131,131,131,28,131,131,131,20,131,131,131,13,131,131,131,8,131,131,131,5,131,131,131,3,131,131,131,2,131,131,131,1,0,0,0,255,131,131,131,38,131,131,131,27,131,131,131,18,131,131,131,11,131,131,131,6,131,131,131,4,131,131,131,2,131,131,131,1
            cornerbottomright: 0,0,0,255,131,131,131,38,131,131,131,27,131,131,131,18,131,131,131,11,131,131,131,6,131,131,131,4,131,131,131,2,131,131,131,1,131,131,131,38,131,131,131,28,131,131,131,20,131,131,131,13,131,131,131,8,131,131,131,5,131,131,131,3,131,131,131,2,131,131,131,1,131,131,131,27,131,131,131,20,131,131,131,15,131,131,131,10,131,131,131,7,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,1,131,131,131,18,131,131,131,13,131,131,131,10,131,131,131,7,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,1,131,131,131,0,131,131,131,11,131,131,131,8,131,131,131,7,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,0,131,131,131,6,131,131,131,5,131,131,131,4,131,131,131,3,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,4,131,131,131,3,131,131,131,3,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,0,131,131,131,2,131,131,131,2,131,131,131,2,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0
            cornerbottomleft: 131,131,131,1,131,131,131,2,131,131,131,4,131,131,131,6,131,131,131,11,131,131,131,18,131,131,131,27,131,131,131,38,0,0,0,255,131,131,131,1,131,131,131,2,131,131,131,3,131,131,131,5,131,131,131,8,131,131,131,13,131,131,131,20,131,131,131,28,131,131,131,38,131,131,131,1,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,7,131,131,131,10,131,131,131,15,131,131,131,20,131,131,131,27,131,131,131,0,131,131,131,1,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,7,131,131,131,10,131,131,131,13,131,131,131,18,131,131,131,0,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,7,131,131,131,8,131,131,131,11,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,3,131,131,131,4,131,131,131,5,131,131,131,6,131,131,131,0,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,3,131,131,131,3,131,131,131,4,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,2,131,131,131,2,131,131,131,2,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,0,131,131,131,1,131,131,131,1,131,131,131,1,131,131,131,1
            */
            _transparencyMasks = new CachedBitmapInfo[]
            {
                new(new byte[] { 131,131,131,2,131,131,131,5,131,131,131,8,131,131,131,11,131,131,131,19,131,131,131,30,131,131,131,46,131,131,131,65,0,0,0,254}, 9, 1),
                new(new byte[] { 131, 131, 131, 2, 131, 131, 131, 5, 131, 131, 131, 8, 131, 131, 131, 11, 131, 131, 131, 19, 131, 131, 131, 30, 131, 131, 131, 46, 131, 131, 131, 65, 0, 0, 0, 254 }, 1, 9),
                new(new byte[] { 0, 0, 0, 254, 131, 131, 131, 65, 131, 131, 131, 46, 131, 131, 131, 30, 131, 131, 131, 19, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 2 }, 9, 1),
                new(new byte[] { 0, 0, 0, 254, 131, 131, 131, 65, 131, 131, 131, 46, 131, 131, 131, 30, 131, 131, 131, 19, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 2 }, 1, 9),
                new(new byte[] { 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 5, 131, 131, 131, 6, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 8, 131, 131, 131, 11, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 13, 131, 131, 131, 18, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 15, 131, 131, 131, 20, 131, 131, 131, 27, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 5, 131, 131, 131, 8, 131, 131, 131, 13, 131, 131, 131, 20, 131, 131, 131, 28, 131, 131, 131, 38, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 4, 131, 131, 131, 6, 131, 131, 131, 11, 131, 131, 131, 18, 131, 131, 131, 27, 131, 131, 131, 38, 0, 0, 0, 255 }, 9, 9),
                new(new byte[] { 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 6, 131, 131, 131, 5, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 18, 131, 131, 131, 13, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 27, 131, 131, 131, 20, 131, 131, 131, 15, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 38, 131, 131, 131, 28, 131, 131, 131, 20, 131, 131, 131, 13, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 0, 0, 0, 255, 131, 131, 131, 38, 131, 131, 131, 27, 131, 131, 131, 18, 131, 131, 131, 11, 131, 131, 131, 6, 131, 131, 131, 4, 131, 131, 131, 2, 131, 131, 131, 1 }, 9, 9),
                new(new byte[] { 0, 0, 0, 255, 131, 131, 131, 38, 131, 131, 131, 27, 131, 131, 131, 18, 131, 131, 131, 11, 131, 131, 131, 6, 131, 131, 131, 4, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 38, 131, 131, 131, 28, 131, 131, 131, 20, 131, 131, 131, 13, 131, 131, 131, 8, 131, 131, 131, 5, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 27, 131, 131, 131, 20, 131, 131, 131, 15, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 18, 131, 131, 131, 13, 131, 131, 131, 10, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 11, 131, 131, 131, 8, 131, 131, 131, 7, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 6, 131, 131, 131, 5, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 4, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0 }, 9, 9),
                new(new byte[] { 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 4, 131, 131, 131, 6, 131, 131, 131, 11, 131, 131, 131, 18, 131, 131, 131, 27, 131, 131, 131, 38, 0, 0, 0, 255, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 5, 131, 131, 131, 8, 131, 131, 131, 13, 131, 131, 131, 20, 131, 131, 131, 28, 131, 131, 131, 38, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 15, 131, 131, 131, 20, 131, 131, 131, 27, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 10, 131, 131, 131, 13, 131, 131, 131, 18, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 7, 131, 131, 131, 8, 131, 131, 131, 11, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 5, 131, 131, 131, 6, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 3, 131, 131, 131, 3, 131, 131, 131, 4, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 2, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 0, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1, 131, 131, 131, 1 }, 9, 9),
            };
        }

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
            var orCreateAlphaMask = GetOrCreateAlphaMask(bitmapPart);
            var glowBitmap = new GlowBitmap(drawingContext.ScreenDC, orCreateAlphaMask.Width, orCreateAlphaMask.Height);
            for (int i = 0; i < orCreateAlphaMask.DIBits.Length; i += BytesPerPixelBgra32)
            {
                byte b = orCreateAlphaMask.DIBits[i + 3];
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

        private static CachedBitmapInfo GetOrCreateAlphaMask(GlowBitmapPart bitmapPart)
        {
            int num = (int)bitmapPart;
            return _transparencyMasks[num];
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
