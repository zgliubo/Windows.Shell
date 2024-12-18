using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using System.Runtime.InteropServices;
using winmdroot = global::Windows.Win32;
using System.Runtime.Versioning;

namespace Windows.Win32
{
    namespace UI.Shell
    {
        /// <summary>Contains information about a system appbar message.</summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-appbardata">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        [global::System.CodeDom.Compiler.GeneratedCode("Microsoft.Windows.CsWin32", "0.3.106+a37a0b4b70")]
        internal partial struct APPBARDATA
        {
            /// <summary>
            /// <para>Type: <b>DWORD</b> The size of the structure, in bytes.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-appbardata#members">Read more on docs.microsoft.com</see>.</para>
            /// </summary>
            internal uint cbSize;

            /// <summary>
            /// <para>Type: <b>HWND</b> The handle to the appbar window. Not all messages use this member. See the individual message page to see if you need to provide an <b>hWind</b> value.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-appbardata#members">Read more on docs.microsoft.com</see>.</para>
            /// </summary>
            internal winmdroot.Foundation.HWND hWnd;

            /// <summary>
            /// <para>Type: <b>UINT</b> An application-defined message identifier. The application uses the specified identifier for notification messages that it sends to the appbar identified by the <b>hWnd</b> member. This member is used when sending the <a href="https://docs.microsoft.com/windows/desktop/shell/abm-new">ABM_NEW</a> message.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-appbardata#members">Read more on docs.microsoft.com</see>.</para>
            /// </summary>
            internal uint uCallbackMessage;

            /// <summary>
            /// <para>Type: <b>UINT</b> A value that specifies an edge of the screen. This member is used when sending one of these messages: </para>
            /// <para>This doc was truncated.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-appbardata#members">Read more on docs.microsoft.com</see>.</para>
            /// </summary>
            internal uint uEdge;

            /// <summary>
            /// <para>Type: <b><a href="https://docs.microsoft.com/windows/desktop/api/windef/ns-windef-rect">RECT</a></b> A <a href="https://docs.microsoft.com/windows/desktop/api/windef/ns-windef-rect">RECT</a> structure whose use varies depending on the message:</para>
            /// <para></para>
            /// <para>This doc was truncated.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-appbardata#members">Read more on docs.microsoft.com</see>.</para>
            /// </summary>
            internal winmdroot.Foundation.RECT rc;

            /// <summary>
            /// <para>Type: <b>LPARAM</b> A message-dependent value. This member is used with these messages: </para>
            /// <para>This doc was truncated.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-appbardata#members">Read more on docs.microsoft.com</see>.</para>
            /// </summary>
            internal winmdroot.Foundation.LPARAM lParam;
        }
    }

    internal static partial class PInvoke
    {
        /// <inheritdoc cref="SHAppBarMessage(uint, winmdroot.UI.Shell.APPBARDATA*)"/>
		[SupportedOSPlatform("windows5.1.2600")]
        internal static unsafe nuint SHAppBarMessage(uint dwMessage, ref winmdroot.UI.Shell.APPBARDATA pData)
        {
            fixed (winmdroot.UI.Shell.APPBARDATA* pDataLocal = &pData)
            {
                nuint __result = PInvoke.SHAppBarMessage(dwMessage, pDataLocal);
                return __result;
            }
        }

        /// <summary>Sends an appbar message to the system.</summary>
        /// <param name="dwMessage">Type: <b>DWORD</b></param>
        /// <param name="pData">
        /// <para>Type: <b>PAPPBARDATA</b> A pointer to an <a href="https://docs.microsoft.com/windows/desktop/api/shellapi/ns-shellapi-appbardata">APPBARDATA</a> structure. The content of the structure on entry and on exit depends on the value set in the <i>dwMessage</i> parameter. See the individual message pages for specifics.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shappbarmessage#parameters">Read more on docs.microsoft.com</see>.</para>
        /// </param>
        /// <returns>
        /// <para>Type: <b>UINT_PTR</b> This function returns a message-dependent value. For more information, see the Windows SDK documentation for the specific appbar message sent. Links to those documents are given in the See Also section.</para>
        /// </returns>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shappbarmessage">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        [DllImport("SHELL32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows5.1.2600")]
        internal static extern unsafe nuint SHAppBarMessage(uint dwMessage, winmdroot.UI.Shell.APPBARDATA* pData);
    }
}
