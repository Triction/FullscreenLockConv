using System;
using System.Diagnostics;
using System.IO;
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
        private bool autoSaveOnExit;
        private bool startAltSearch;
        private bool startExtended;
        private bool startMuted;
        private bool startPaused;
        private bool startPinned;
        private double startPollingRate;
        private string startSearchTarget;
        private bool rememberSearchTarget;

        // Run-time checks
        // AltSearch is Process Search mode
        private bool isAltSearch;
        private bool isExtended;
        private bool isMouseCaptured;
        private bool isMuted;
        private bool isPaused;
        private bool isPinned;

        private bool isDisposed;

        internal AppConfig configFile = new AppConfig();
        internal string configFileLocation;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            configFileLocation = Directory.GetCurrentDirectory() + "\\" + DEFAULT_CONFIG;
            configFile = ReadConfigFile(configFileLocation);
            this.SetPlacement(configFile.MainWindowPlacement);
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

        private void WriteConfigFile(string fileLocation)
        {
            using (FileStream fileStream = new FileStream(fileLocation, 
                File.Exists(fileLocation) ? FileMode.Truncate : FileMode.CreateNew, 
                FileAccess.Write))
            {
                using (StreamWriter streamWriter = new StreamWriter(fileStream))
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string jsonString = JsonSerializer.Serialize(configFile, options);
                    streamWriter.Write(jsonString);
                }
            }
        }

        private AppConfig ReadConfigFile(string fileLocation)
        {
            if (!File.Exists(fileLocation))
            {
                // Create a new config as we didn't find one.
                WriteConfigFile(fileLocation);
                LogToConsole(DEFAULT_CONFIG + " not found, creating a new one", true);
            }
                

            using (FileStream fileStream = new FileStream(fileLocation,
                FileMode.Open,
                FileAccess.Read))
            {
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    var options = new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                        IgnoreNullValues = true,
                    };
                    string jsonString = streamReader.ReadToEnd();
                    return JsonSerializer.Deserialize<AppConfig>(jsonString);
                }
            }
        }

        private void ReadSettings()
        {
            autoSaveOnExit = configFile.AutoSaveLastUsedOptions;
            startAltSearch = configFile.StartInProcessSearchMode;
            startExtended = configFile.StartInExtendedMode;
            startMuted = configFile.StartInMutedMode;
            startPaused = configFile.StartInPausedMode;
            startPinned = configFile.StartInPinnedMode;
            startPollingRate = configFile.TimerPollingRate;
            startSearchTarget = configFile.LastKnownSearchTarget;
            rememberSearchTarget = configFile.RememberSearchTarget;
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
            ReadSettings();
            LogToConsole("Settings read from " + DEFAULT_CONFIG, true);
            
            // Handle settings
            // AutoSaveLastUsedOptions Log
            LogToConsole("Loaded setting - AutoSaveLastUsedOptions: " + autoSaveOnExit, true);

            // StartInExtendedMode
            if (startExtended)
            {
                btnToggleWindowExtension.Content = iconExtended;
                btnToggleWindowExtension.ToolTip = toolTipExtended;
                isExtended = true;
            }
            this.Height = isExtended ? 416 : 230;
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

        private void UpdateStatusLabel(string strText)
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
            OptionsWindow optionsWindow = new OptionsWindow(configFile)
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

        internal void LogToConsole(string strText, bool bLogAnyway = false)
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

        private string GetSearchProcessName()
        {
            string temp = "";
            Dispatcher.Invoke(() =>
            {
                temp = txtSearchProcessName.Text;
            });
            return temp;
        }

        private void UpdateAutoSavedSettings()
        {
            // The only accessible settings that we can touch as autoSave are: Extended, Muted, Paused, Search, Pinned, and LastProcessTarget.
            // Rest must be handled via the Options dialog.

            // Maybe disable this from the autoSave due to potential annoyance.
            configFile.StartInPausedMode = isPaused;

            configFile.StartInProcessSearchMode = isAltSearch;
            configFile.StartInExtendedMode = isExtended;
            configFile.StartInMutedMode = isMuted;
            configFile.StartInPinnedMode = isPinned;
            configFile.RememberSearchTarget = rememberSearchTarget;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timer.Stop();

            if (autoSaveOnExit)
                UpdateAutoSavedSettings();

            if (rememberSearchTarget)
                configFile.LastKnownSearchTarget = txtSearchProcessName.Text;

            configFile.MainWindowPlacement = this.GetPlacement();
            WriteConfigFile(configFileLocation);

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
            // Show the About dialog
            string strVersion = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
            string messageText = "Version: " + strVersion + "\n\nBased upon the code by: ✨ Blåberry ✨\nUpdated by: Triction" +
                "\n\nIcons provided by: Material Design Icons\nhttps://materialdesignicons.com/";
            string caption = "About";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage image = MessageBoxImage.Information;
            CustomMessageBox.Show(this, messageText, caption, button, image);
        }
    }
}
