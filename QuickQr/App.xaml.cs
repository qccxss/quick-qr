using System.Windows;
using System;

namespace QuickQr
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var window = new MainWindow();
                MainWindow = window;
                window.Show();
            }
            catch (Exception)
            {
                MessageBox.Show("Quick QR could not start. Please rebuild or reinstall the application.", "Quick QR could not start", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
