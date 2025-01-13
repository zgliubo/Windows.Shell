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
using Microsoft.Win32;
using Windows.Win32.System.Registry;
using System.Xml.Linq;
using static Windows.Shell.WindowEdge;

namespace Windows.Shell
{
    class WindowEdge
    {
        private readonly List<ResizeBorderWindow> borders;
        private readonly EdgeMode[] _edgeModes;
        private EdgeMode _mode;

        public EdgeMode Mode => _mode;

        public WindowEdge(RECT position, HWND parent, params EdgeMode[] modes)
        {
            _mode = modes.First();
            _edgeModes = modes;
            borders = CreateWindows(position, parent, _mode);
        }

        public void UpdatePositionWidthActive(RECT position, bool isActive)
        {
            borders.ForEach(x => x.UpdatePositionWidthActive(position, isActive));
        }

        public void UpdatePosition(RECT position)
        {
            borders.ForEach(x => x.UpdatePosition(position));
        }

        public void Show()
        {
            borders.ForEach(x => x.Show());
        }

        public void Hide()
        {
            borders.ForEach(x => x.Hide());
        }

        public void Close()
        {
            borders.ForEach(x => x.Close());
            borders.Clear();
        }

        private static List<ResizeBorderWindow> CreateWindows(RECT position, HWND parent, EdgeMode mode)
        {
            return new List<ResizeBorderWindow>
            {
                new(position, parent, Dock.Left, mode),
                new(position, parent, Dock.Top, mode),
                new(position, parent, Dock.Right, mode),
                new(position, parent, Dock.Bottom, mode),
            };
        }

        public void UpdateToNextMode(RECT position, HWND parent, bool isActive)
        {
            if (_edgeModes.Length == 1) return;

            var index = _edgeModes.ToList().IndexOf(_mode) + 1;
            if (index == _edgeModes.Length - 1) index = 0;

            _mode = _edgeModes[index];

            borders.ForEach(x => x.Close());
            borders.Clear();
            borders.AddRange(CreateWindows(default, parent, _mode));
            borders.ForEach(x =>
            {
                x.UpdatePositionWidthActive(position, isActive);
                x.Show();
            });
        }

        public enum Dock
        {
            Left,
            Top,
            Right,
            Bottom
        }

        public enum EdgeMode
        {
            /// <summary>
            /// 仅边框线
            /// </summary>
            Border,
            /// <summary>
            /// 仅调整大小(全透明)
            /// </summary>
            Resize,
            /// <summary>
            ///  边框线和调整大小(Resize部分透明)
            /// </summary>
            BorderResize,
            /// <summary>
            /// 边框线和调整大小(包含阴影)
            /// </summary>
            BorderResizeWidthShadow
        }
    }
}
