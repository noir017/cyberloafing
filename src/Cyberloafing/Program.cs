using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Cyberloafing
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length == 1 && args[0] == "--smoke")
            {
                Environment.Exit(Smoke.Run());
                return;
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, @"Local\CyberloafingReader.v1", out createdNew))
            {
                if (!createdNew)
                    return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    try
                    {
                        File.WriteAllText(AppPaths.Error, e.Exception.ToString(), Encoding.UTF8);
                        MessageBox.Show(e.Exception.Message, "摸鱼阅读");
                    }
                    catch
                    {
                    }
                };
                Application.Run(new ReaderForm());
                GC.KeepAlive(mutex);
            }
        }
    }
}
