using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FullscreenLockConv
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        // Old settings values - for comparison
        bool oldAutoSave;
        bool oldExtended;
        bool oldMuted;
        bool oldPaused;
        bool oldSearch;
        string oldPolling;
        bool oldPinned;
        bool oldSearchTarget;

        // UI Content
        Viewbox iconCloseSaved;
        Viewbox iconCloseUnsaved;
        Viewbox iconSaveSaved;
        Viewbox iconSaveUnsaved;

        AppConfig configFile;
        DispatcherTimer dispatcherTimer;

        public OptionsWindow(AppConfig config)
        {
            InitializeComponent();
            configFile = config;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            iconCloseSaved = TryFindResource("IconCloseSaved") as Viewbox;
            iconCloseUnsaved = TryFindResource("IconCloseUnsaved") as Viewbox;
            iconSaveSaved = TryFindResource("IconSaveSaved") as Viewbox;
            iconSaveUnsaved = TryFindResource("IconSaveUnsaved") as Viewbox;

            oldAutoSave = configFile.AutoSaveLastUsedOptions;
            oldExtended = configFile.StartInExtendedMode;
            oldMuted = configFile.StartInMutedMode;
            oldPaused = configFile.StartInPausedMode;
            oldSearch = configFile.StartInProcessSearchMode;
            oldPolling = Convert.ToString(configFile.TimerPollingRate, System.Globalization.CultureInfo.CurrentCulture);
            oldPinned = configFile.StartInPinnedMode;
            oldSearchTarget = configFile.RememberSearchTarget;

            chkAutoSave.IsChecked = oldAutoSave;
            chkExtended.IsChecked = oldExtended;
            chkMuted.IsChecked = oldMuted;
            chkPaused.IsChecked = oldPaused;
            chkProcess.IsChecked = oldSearch;
            txtPollingRate.Text = oldPolling;
            chkTopmost.IsChecked = oldPinned;
            chkSearchTarget.IsChecked = oldSearchTarget;

            EnableStartUpOptions(!oldAutoSave);

            dispatcherTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            bool settingsChanged = SettingsChanged();
            btnCancel.Content = settingsChanged ? iconCloseUnsaved : iconCloseSaved;
            btnSave.Content = settingsChanged ? iconSaveUnsaved : iconSaveSaved;
        }

        private void EnableStartUpOptions(bool enabled)
        {
            chkExtended.IsEnabled = enabled;
            chkMuted.IsEnabled = enabled;
            chkProcess.IsEnabled = enabled;
            chkPaused.IsEnabled = enabled;
            chkTopmost.IsEnabled = enabled;
        }

        private bool SettingsChanged()
        {
            if (txtPollingRate.Text != oldPolling) return true;
            if (chkAutoSave.IsChecked != oldAutoSave) return true;
            if (chkSearchTarget.IsChecked != oldSearchTarget) return true;
            if (!(bool)chkAutoSave.IsChecked)
            {
                if (chkExtended.IsChecked != oldExtended) return true;
                if (chkMuted.IsChecked != oldMuted) return true;
                if (chkPaused.IsChecked != oldPaused) return true;
                if (chkProcess.IsChecked != oldSearch) return true;
                if (chkTopmost.IsChecked != oldPinned) return true;
            }
            return false;
        }

        private static void ChangeStyle(Control control, bool bChanged)
        {
            control.FontStyle = bChanged ? FontStyles.Italic : FontStyles.Normal;
            control.FontWeight = bChanged ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private void ChkAutoSave_Click(object sender, RoutedEventArgs e)
        {
            EnableStartUpOptions(!(bool)chkAutoSave.IsChecked);
            ChangeStyle(e.Source as CheckBox, chkAutoSave.IsChecked != oldAutoSave);
        }

        private void ChkExtended_Click(object sender, RoutedEventArgs e)
        {
            ChangeStyle(e.Source as CheckBox, chkExtended.IsChecked != oldExtended);
        }

        private void ChkMuted_Click(object sender, RoutedEventArgs e)
        {
            ChangeStyle(e.Source as CheckBox, chkMuted.IsChecked != oldMuted);
        }

        private void ChkProcess_Click(object sender, RoutedEventArgs e)
        {
            ChangeStyle(e.Source as CheckBox, chkProcess.IsChecked != oldSearch);
        }

        private void ChkPaused_Click(object sender, RoutedEventArgs e)
        {
            ChangeStyle(e.Source as CheckBox, chkPaused.IsChecked != oldPaused);
        }

        private void TxtPollingRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool changed = !txtPollingRate.Text.Equals(oldPolling, StringComparison.Ordinal);
            ChangeStyle(e.Source as TextBox, changed);
            ChangeStyle(lblPollingRate as Label, changed);
        }

        private void TxtPollingRate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            configFile.AutoSaveLastUsedOptions = (bool)chkAutoSave.IsChecked;
            configFile.StartInExtendedMode = (bool)chkExtended.IsChecked;
            configFile.StartInMutedMode = (bool)chkMuted.IsChecked;
            configFile.StartInPausedMode = (bool)chkPaused.IsChecked;
            configFile.StartInProcessSearchMode = (bool)chkProcess.IsChecked;
            configFile.TimerPollingRate = Convert.ToDouble(txtPollingRate.Text.Replace(" ", ""), System.Globalization.CultureInfo.CurrentCulture);
            configFile.StartInPinnedMode = (bool)chkTopmost.IsChecked;
            configFile.RememberSearchTarget = (bool)chkSearchTarget.IsChecked;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            dispatcherTimer.Stop();
        }

        private void ChkTopmost_Click(object sender, RoutedEventArgs e)
        {
            ChangeStyle(e.Source as CheckBox, chkTopmost.IsChecked != oldPinned);
        }

        private void ChkSearchTarget_Click(object sender, RoutedEventArgs e)
        {
            ChangeStyle(e.Source as CheckBox, chkSearchTarget.IsChecked != oldSearchTarget);
        }
    }
}
