using Windows.Win32.Foundation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "CallWindowProcW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows5.0")]
        internal static extern LRESULT CallWindowProc(nint lpPrevWndFunc, HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);
    }
}
