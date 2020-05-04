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

        DispatcherTimer dispatcherTimer;

        public OptionsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            iconCloseSaved = TryFindResource("IconCloseSaved") as Viewbox;
            iconCloseUnsaved = TryFindResource("IconCloseUnsaved") as Viewbox;
            iconSaveSaved = TryFindResource("IconSaveSaved") as Viewbox;
            iconSaveUnsaved = TryFindResource("IconSaveUnsaved") as Viewbox;

            oldAutoSave = Properties.Settings.Default.AutoSaveLastUsedOptions;
            oldExtended = Properties.Settings.Default.StartInExtendedMode;
            oldMuted = Properties.Settings.Default.StartInMutedMode;
            oldPaused = Properties.Settings.Default.StartInPausedMode;
            oldSearch = Properties.Settings.Default.StartInProcessSearchMode;
            oldPolling = Convert.ToString(Properties.Settings.Default.TimerPollingRate, System.Globalization.CultureInfo.CurrentCulture);
            oldPinned = Properties.Settings.Default.StartInPinnedMode;
            oldSearchTarget = Properties.Settings.Default.RememberSearchTarget;

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
            Properties.Settings.Default.AutoSaveLastUsedOptions = (bool)chkAutoSave.IsChecked;
            Properties.Settings.Default.StartInExtendedMode = (bool)chkExtended.IsChecked;
            Properties.Settings.Default.StartInMutedMode = (bool)chkMuted.IsChecked;
            Properties.Settings.Default.StartInPausedMode = (bool)chkPaused.IsChecked;
            Properties.Settings.Default.StartInProcessSearchMode = (bool)chkProcess.IsChecked;
            Properties.Settings.Default.TimerPollingRate = Convert.ToDouble(txtPollingRate.Text.Replace(" ", ""), System.Globalization.CultureInfo.CurrentCulture);
            Properties.Settings.Default.StartInPinnedMode = (bool)chkTopmost.IsChecked;
            Properties.Settings.Default.RememberSearchTarget = (bool)chkSearchTarget.IsChecked;

            Properties.Settings.Default.Save();

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
