using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading;
using System.Windows.Forms;

namespace TapShareBLEServer
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                Thread thread = new Thread(() => Form1.handleToastEvent(toastArgs.Argument));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                //form.handleToastEvent(toastArgs.Argument);
            };
            Application.Run(new Form1());
        }
    }
}
