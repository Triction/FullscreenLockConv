// Part of: https://stackoverflow.com/a/19326

using Microsoft.VisualBasic.ApplicationServices;

namespace FullscreenLockConv
{
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private SingleInstanceApplication _app;

        public SingleInstanceManager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            _app = new SingleInstanceApplication();
            _app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary()
            {
                Source = new System.Uri("pack://application:,,,/Resources/IconsRes.xaml")
            });
            _app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary()
            { 
                Source = new System.Uri("pack://application:,,,/Resources/StringsRes.xaml")
            });
            _app.Run();
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            base.OnStartupNextInstance(eventArgs);
            _app.Activate();
        }
    }
}
