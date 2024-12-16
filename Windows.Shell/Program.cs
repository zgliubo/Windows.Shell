using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

namespace Windows.Shell
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var window = new Window();
            window.CreateWindow();
            while (PInvoke.GetMessage(out MSG msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
    }
}
