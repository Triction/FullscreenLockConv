// Part of: https://stackoverflow.com/a/19326

using System.Windows;

namespace FullscreenLockConv
{
    public class SingleInstanceApplication : Application
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        public void Activate()
        {
            MainWindow.Activate();
        }
    }
}
