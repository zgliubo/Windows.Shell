using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

namespace Windows.Shell
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 捕获未处理的异常
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine($"Unhandled Exception: {e.ExceptionObject}");
                // 可以记录日志或退出应用程序
            };

            // 捕获未处理的任务异常
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Console.WriteLine($"Unobserved Task Exception: {e.Exception}");
                e.SetObserved(); // 防止应用程序崩溃
            };

            var window = new Window();
            window.Show();
            while (PInvoke.GetMessage(out MSG msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
    }
}
