using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using System.Runtime.InteropServices;


namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        [DllImport("USER32.dll", SetLastError = true)]
        private static extern nint SetWindowLongW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

        [DllImport("USER32.dll", SetLastError = true)]
        private static extern nint SetWindowLongPtrW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

        /// <summary>
        /// Changes an attribute of the specified window. The function also sets a value at the specified offset in the extra window memory.
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setwindowlongptrw">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        public static nint SetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint newValue)
        {
            nint result = Environment.Is64BitProcess ? SetWindowLongPtrW(hWnd, nIndex, newValue) : SetWindowLongW(hWnd, nIndex, (int)newValue);
            return result;
        }
    }
}
