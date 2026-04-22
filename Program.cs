using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinClip
{
    static class Program
    {
        /// <summary>Entry.</summary>
        [STAThread]
        static void Main()
        {
            // Handle unexpected esceptions.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => { HandleException(e.Exception, "UI Thread Exception"); };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => { HandleException((Exception)e.ExceptionObject, "Background Thread Exception"); };

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static void HandleException(Exception ex, string type) 
        {
            MessageBox.Show(ex.ToString(), type);
            Environment.Exit(1);
        }
    }
}
