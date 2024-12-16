using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using System.Runtime.InteropServices;


namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        [DllImport("USER32.dll", SetLastError = true)]
        private static extern nint GetWindowLongW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [DllImport("USER32.dll", SetLastError = true)]
        private static extern nint GetWindowLongPtrW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        public static nint GetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex)
        {
            nint result = Environment.Is64BitProcess
                ? GetWindowLongPtrW(hWnd, nIndex)
                : GetWindowLongW(hWnd, nIndex);
            return result;
        }
    }
}
