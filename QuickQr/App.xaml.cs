using System.Windows;
using System;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace QuickQr
{
    public partial class App : Application
    {
        private Forms.NotifyIcon trayIcon;

        protected override void OnExit(ExitEventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnExit(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var window = new MainWindow();
                MainWindow = window;
                trayIcon = new Forms.NotifyIcon
                {
                    Text = "Quick QR",
                    Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    Visible = true
                };
                var menu = new Forms.ContextMenuStrip();
                menu.Items.Add("Open Quick QR", null, (sender, args) => ShowMainWindow());
                menu.Items.Add(new Forms.ToolStripSeparator());
                menu.Items.Add("Exit", null, (sender, args) => ExitApplication());
                trayIcon.ContextMenuStrip = menu;
                trayIcon.DoubleClick += (sender, args) => ShowMainWindow();
                window.Show();
            }
            catch (Exception)
            {
                MessageBox.Show("Quick QR could not start. Please rebuild or reinstall the application.", "Quick QR could not start", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void ShowMainWindow()
        {
            if (MainWindow is MainWindow window)
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
            }
        }

        private void ExitApplication()
        {
            if (MainWindow is MainWindow window)
            {
                window.AllowClose = true;
                window.Close();
            }
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Shutdown();
        }
    }
}
