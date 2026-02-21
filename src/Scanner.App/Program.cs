using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;
using WinRT;

namespace Scanner.App;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            ComWrappersSupport.InitializeComWrappers();
            Microsoft.UI.Xaml.Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        catch (Exception ex)
        {
            ShowFatalError(
                $"Scanner failed to start.\n\n{ex.Message}\n\n" +
                $"If this problem persists, try reinstalling the application.");
        }
    }

    static void ShowFatalError(string message)
    {
        _ = MessageBox(IntPtr.Zero, message, "Scanner – Fatal Error", MB_ICONERROR | MB_OK);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    const uint MB_OK = 0x0;
    const uint MB_ICONERROR = 0x10;
}
