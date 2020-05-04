using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace FullscreenLockConv
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        // Constants
        private const string DEFAULT_CONFIG = "config.json";

        // Process watcher variables
        private Timer timer;
        private Process capturedProcess;

        // UI Content
        // Storyboard Animations
        private Storyboard extendWindow;
        private Storyboard collapseWindow;
        // Toggle Audio
        private Viewbox iconMuted;
        private Viewbox iconUnmuted;
        private string toolTipMuted;
        private string toolTipUnmuted;
        // Toggle UI Expansion
        private Viewbox iconCollapsed;
        private Viewbox iconExtended;
        private string toolTipCollapsed;
        private string toolTipExtended;
        // Toggle Timer
        private Viewbox iconPaused;
        private Viewbox iconUnpaused;
        private string toolTipPaused;
        private string toolTipUnpaused;
        // Toggle Topmost
        private Viewbox iconPinned;
        private Viewbox iconUnpinned;
        private string toolTipPinned;
        private string toolTipUnpinned;

        // Start up and settings
        bool autoSaveOnExit;
        bool startAltSearch;
        bool startExtended;
        bool startMuted;
        bool startPaused;
        bool startPinned;
        double startPollingRate;
        string startSearchTarget;
        bool rememberSearchTarget;

        // Run-time checks
        // AltSearch is Process Search mode
        bool isAltSearch;
        bool isExtended;
        bool isMouseCaptured;
        bool isMuted;
        bool isPaused;
        bool isPinned;

        private bool isDisposed;

        private AppConfig appConfig;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.SetPlacement(Properties.Settings.Default.MainWindowPlacement);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                capturedProcess.Dispose();
                timer.Dispose();
            }

            isDisposed = true;
        }

        private void ReadSettings()
        {
            autoSaveOnExit = Properties.Settings.Default.AutoSaveLastUsedOptions;
            startAltSearch = Properties.Settings.Default.StartInProcessSearchMode;
            startExtended = Properties.Settings.Default.StartInExtendedMode;
            startMuted = Properties.Settings.Default.StartInMutedMode;
            startPaused = Properties.Settings.Default.StartInPausedMode;
            startPinned = Properties.Settings.Default.StartInPinnedMode;
            startPollingRate = Properties.Settings.Default.TimerPollingRate;
            startSearchTarget = Properties.Settings.Default.LastKnownSearchTarget;
            rememberSearchTarget = Properties.Settings.Default.RememberSearchTarget;
        }

        private void ReadConfigFile(AppConfig config)
        {
            /*using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (!isolatedStorage.FileExists(DEFAULT_CONFIG))
                {
                    using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(DEFAULT_CONFIG, FileMode.CreateNew, isolatedStorage))
                    {
                        using (StreamWriter writer = new StreamWriter(fileStream))
                        {
                            var options = new JsonSerializerOptions
                            {
                                WriteIndented = true
                            };
                            string jsonString = JsonSerializer.Serialize(new AppConfig(), options);
                            writer.Write(jsonString);
                        }
                    }
                }

            }*/
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine(config.GetFullPath());

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up UI content
            extendWindow = TryFindResource("ExtendWindow") as Storyboard;
            collapseWindow = TryFindResource("CollapseWindow") as Storyboard;
            iconMuted = TryFindResource("IconMuted") as Viewbox;
            iconUnmuted = TryFindResource("IconUnmuted") as Viewbox;
            toolTipMuted = TryFindResource("ToolTipMuted") as string;
            toolTipUnmuted = TryFindResource("ToolTipUnmuted") as string;
            iconCollapsed = TryFindResource("IconCollapsed") as Viewbox;
            iconExtended = TryFindResource("IconExtended") as Viewbox;
            toolTipCollapsed = TryFindResource("ToolTipCollapsed") as string;
            toolTipExtended = TryFindResource("ToolTipExtended") as string;
            iconPaused = TryFindResource("IconPaused") as Viewbox;
            iconUnpaused = TryFindResource("IconUnpaused") as Viewbox;
            toolTipPaused = TryFindResource("ToolTipPaused") as string;
            toolTipUnpaused = TryFindResource("ToolTipUnpaused") as string;
            iconPinned = TryFindResource("IconPinned") as Viewbox;
            iconUnpinned = TryFindResource("IconUnpinned") as Viewbox;
            toolTipPinned = TryFindResource("ToolTipPinned") as string;
            toolTipUnpinned = TryFindResource("ToolTipUnpinned") as string;

            // Read settings
            appConfig = new AppConfig(DEFAULT_CONFIG);
            ReadConfigFile(appConfig);
            ReadSettings();

            // Handle settings
            // AutoSaveLastUsedOptions Log
            LogToConsole("Loaded setting - AutoSaveLastUsedOptions: " + autoSaveOnExit, true);

            // StartInExtendedMode
            if (startExtended)
            {
                btnToggleWindowExtension.Content = iconExtended;
                btnToggleWindowExtension.ToolTip = toolTipExtended;
                this.Height = 416;
                isExtended = true;
            }
            LogToConsole("Loaded setting - StartInExtendedMode: " + startExtended, true);

            // StartInMutedMode
            if (startMuted)
            {
                btnMute.Content = iconMuted;
                btnMute.ToolTip = toolTipMuted;
                isMuted = true;
            }
            LogToConsole("Loaded setting - StartInMutedMode: " + startMuted, true);

            // StartInProcessSearchMode
            if (startAltSearch)
                rdbProcess.IsChecked = true;
            else
                rdbForeground.IsChecked = true;

            isAltSearch = startAltSearch;
            LogToConsole("Loaded setting - StartInProcessSearchMode: " + startAltSearch, true);

            // RememberSearchTarget
            LogToConsole("Loaded setting - RememberSearchTarget: " + rememberSearchTarget, true);
            if (rememberSearchTarget)
            {
                // LastKnownSearchTarget
                txtSearchProcessName.Text = startSearchTarget;
                LogToConsole("Loaded setting - LastKnownSearchTarget: " + startSearchTarget, true);
            }   

            // StartInPausedMode
            if (startPaused)
            {
                btnPause.Content = iconPaused;
                btnPause.ToolTip = toolTipPaused;
                isPaused = true;
            }
            UpdateStatusLabel(startPaused ? "Paused" : "Waiting");
            LogToConsole("Loaded setting - StartInPausedMode: " + startPaused, true);

            // StartInPinnedMode
            if (startPinned)
            {
                btnToggleTopMost.Content = iconPinned;
                btnToggleTopMost.ToolTip = toolTipPinned;
                isPinned = true;
            }
            LogToConsole("Loaded setting - StartInPinnedMode: " + startPinned);

            // TimerPollingRate Log
            LogToConsole("Loaded setting - TimerPollingRate: " + startPollingRate, true);
            // Finished loading settings

            // Launch timer last
            timer = new Timer(startPollingRate);
            timer.Elapsed += Timer_Elapsed;
            if (!startPaused)
                timer.Start();

            LogToConsole("Timer initialized" + (timer.Enabled ? " and started" : "") + " with interval of " + timer.Interval + "ms", true);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsForegroundFullscreen())
                UpdateStatusLabel(isExtended ? "Captured" : "Captured " + capturedProcess.ProcessName);
            else
                UpdateStatusLabel("Waiting");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "<Pending>")]
        private bool IsForegroundFullscreen()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            if (hWnd != null && !hWnd.Equals(IntPtr.Zero))
            {
                IntPtr hDesktop = NativeMethods.GetDesktopWindow();
                IntPtr hShell = NativeMethods.GetShellWindow();
                if (!(hWnd.Equals(hDesktop) || hWnd.Equals(hShell)))
                {
                    _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint procId);
                    capturedProcess = Process.GetProcessById((int)procId);
                    if (isAltSearch && !isMouseCaptured)
                    {
                        string[] tempStrings = GetSearchProcessName().Trim().Split('.');
                        if (!capturedProcess.ProcessName.Equals(tempStrings[0]))
                            return false;
                    }
                    _ = NativeMethods.GetWindowRect(hWnd, out AltRect fgBounds);
                    AltRect scrnBounds = System.Windows.Forms.Screen.FromHandle(hWnd).Bounds;
                    if ((fgBounds.Bottom - fgBounds.Top) == scrnBounds.Height &&
                        (fgBounds.Right - fgBounds.Left) == scrnBounds.Width)
                    {
                        if (!isMouseCaptured)
                        {
                            isMouseCaptured = true;
                            if (!isMuted)
                            {
                                NativeMethods.Beep(500, 20);
                                NativeMethods.Beep(700, 20);
                            }
                            LogToConsole("Captured mouse cursor to");
                            LogToConsole("process: " + capturedProcess.ProcessName);
                            LogToConsole("title: " + capturedProcess.MainWindowTitle);
                        }
                        NativeMethods.ClipCursor(ref scrnBounds);
                        return true;
                    }
                    else
                    {
                        if (isMouseCaptured)
                        {
                            isMouseCaptured = false;
                            if (!isMuted)
                            {
                                NativeMethods.Beep(700, 20);
                                NativeMethods.Beep(500, 20);
                            }
                            LogToConsole("Lost focus from captured window");
                        }
                        NativeMethods.ClipCursor(IntPtr.Zero);
                        return false;
                    }
                }
            }
            return false;
        }

        private void ToggleSearchProcessControls(bool enabled)
        {
            lblSearchProcess.IsEnabled = enabled;
            txtSearchProcessName.IsEnabled = enabled;
        }

        private void ReleaseWindowResizeButtonLock(object sender, EventArgs e)
        {
            isExtended = !isExtended;
            btnToggleWindowExtension.Content = isExtended ? iconExtended : iconCollapsed;
            btnToggleWindowExtension.ToolTip = isExtended ? toolTipExtended : toolTipCollapsed;
            btnToggleWindowExtension.IsEnabled = true;
        }

        private void BtnToggleWindowExtension_Click(object sender, RoutedEventArgs e)
        {
            btnToggleWindowExtension.IsEnabled = false;
            BeginStoryboard(isExtended ? collapseWindow : extendWindow);
        }

        public void UpdateStatusLabel(string strText)
        {
            Dispatcher.Invoke(() =>
            {
                lblStatus.Content = strText;
            });
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
            if (isPaused)
                timer.Stop();
            else
                timer.Start();

            btnPause.Content = isPaused ? iconPaused : iconUnpaused;
            btnPause.ToolTip = isPaused ? toolTipPaused : toolTipUnpaused;
            UpdateStatusLabel(isPaused ? "Paused" : "Waiting");
            LogToConsole(isPaused ? "Paused, no longer watching for window" : "Unpaused, waiting for window focus");
        }

        private void BtnOptions_Click(object sender, RoutedEventArgs e)
        {
            // Temp pause timer whilst Options dialog is open
            bool optionsPause = false;
            if (!isPaused)
            {
                optionsPause = true;
                BtnPause_Click(sender, e);
            }

            // Options time
            OptionsWindow optionsWindow = new OptionsWindow
            {
                ShowInTaskbar = false,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if ((bool)optionsWindow.ShowDialog())
                ReadSettings();

            // Stop temp pause if enabled
            if (optionsPause)
                BtnPause_Click(sender, e);
        }

        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            isMuted = !isMuted;
            btnMute.Content = isMuted ? iconMuted : iconUnmuted;
            btnMute.ToolTip = isMuted ? toolTipMuted : toolTipUnmuted;
            LogToConsole(isMuted ? "Muted audio" : "Unmuted audio");
        }

        public void LogToConsole(string strText, bool bLogAnyway = false)
        {
            if (isExtended || bLogAnyway)
            {
                Dispatcher.Invoke(() =>
                {
                    txtConsoleOutput.AppendText("> " + strText + "\n");
                    txtConsoleOutput.ScrollToEnd();
                });
            }
        }

        public string GetSearchProcessName()
        {
            string temp = "";
            Dispatcher.Invoke(() =>
            {
                temp = txtSearchProcessName.Text;
            });
            return temp;
        }

        public void AutoSavedSettings()
        {
            // The only accessible settings that we can touch as autoSave are: Extended, Muted, Paused, Search, Pinned, and LastProcessTarget.
            // Rest must be handled via the Options dialog.

            Properties.Settings.Default.StartInProcessSearchMode = isAltSearch;
            Properties.Settings.Default.StartInExtendedMode = isExtended;
            Properties.Settings.Default.StartInMutedMode = isMuted;
            Properties.Settings.Default.StartInPausedMode = isPaused; // Maybe disable this from the autoSave due to potential annoyance.
            Properties.Settings.Default.StartInPinnedMode = isPinned;
            Properties.Settings.Default.RememberSearchTarget = rememberSearchTarget;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timer.Stop();

            if (autoSaveOnExit)
                AutoSavedSettings();

            if (rememberSearchTarget)
                Properties.Settings.Default.LastKnownSearchTarget = GetSearchProcessName();

            Properties.Settings.Default.MainWindowPlacement = this.GetPlacement();
            Properties.Settings.Default.Save();

            NativeMethods.ClipCursor(IntPtr.Zero);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            Environment.Exit(0);
        }

        private void RdbSearchMode_Checked(object sender, RoutedEventArgs e)
        {
            isAltSearch = (bool)rdbProcess.IsChecked;
            ToggleSearchProcessControls(isAltSearch);
        }

        private void BtnToggleTopMost_Click(object sender, RoutedEventArgs e)
        {
            isPinned = !isPinned;
            this.Topmost = isPinned;
            btnToggleTopMost.Content = isPinned ? iconPinned : iconUnpinned;
            btnToggleTopMost.ToolTip = isPinned ? toolTipPinned : toolTipUnpinned;
            LogToConsole("Windows is now " + (isPinned ? "" : "un") + "pinned");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void About_Click(object sender, RoutedEventArgs e)
        {
            string strVersion = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
            // Show the About dialog
            string messageText = "Version: " + strVersion + "\n\nBased upon the code by: ✨ Blåberry ✨\nUpdated by: Triction" +
                "\n\nIcons provided by: Material Design Icons\nhttps://materialdesignicons.com/";
            string caption = "About";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage image = MessageBoxImage.Information;
            //MessageBox.Show(this, messageText, caption, button, image);
            // Replace with custom implemented MessageBox
            CustomMessageBox.Show(this, messageText, caption, button, image);
        }
    }
}
