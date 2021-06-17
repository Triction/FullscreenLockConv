using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        private static Timer timer;
        private Process capturedProcess;

        // Background Save timer
        private static Timer fileTimer;

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
        private bool hasSaved = true;
        private bool startUp = true;

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
            this.SetPlacement(configFile.WindowPosition);
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
                fileTimer.Dispose();
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
            autoSaveOnExit = configFile.AutoSaveConfig;
            startAltSearch = configFile.ProcessSearchMode;
            startExtended = configFile.ExtendedMode;
            startMuted = configFile.MutedMode;
            startPaused = configFile.PausedMode;
            startPinned = configFile.PinnedMode;
            startPollingRate = configFile.TimerInterval;
            startSearchTarget = configFile.Process;
            rememberSearchTarget = configFile.SaveProcessName;
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
            LogToConsole("Read settings from " + DEFAULT_CONFIG, true);

            // Handle settings
            // AutoSaveLastUsedOptions Log
            string loadedSettings = "";
            loadedSettings += "Loaded setting - AutoSaveConfig: " + autoSaveOnExit;

            // StartInExtendedMode
            if (startExtended)
            {
                btnToggleWindowExtension.Content = iconExtended;
                btnToggleWindowExtension.ToolTip = toolTipExtended;
                isExtended = true;
            }
            // TODO: convert height variables to constants for both UI and backend.
            this.Height = isExtended ? 416 : 230;
            loadedSettings += "\nLoaded setting - ExtendedMode: " + startExtended;

            // StartInMutedMode
            if (startMuted)
            {
                btnMute.Content = iconMuted;
                btnMute.ToolTip = toolTipMuted;
                isMuted = true;
            }
            loadedSettings += "\nLoaded setting - MutedMode: " + startMuted;

            // StartInProcessSearchMode
            if (startAltSearch)
                rdbProcess.IsChecked = true;
            else
                rdbForeground.IsChecked = true;

            isAltSearch = startAltSearch;
            loadedSettings += "\nLoaded setting - ProcessSearchMode: " + startAltSearch;

            // RememberSearchTarget
            loadedSettings += "\nLoaded setting - SaveProcessName: " + rememberSearchTarget;
            if (rememberSearchTarget)
            {
                // LastKnownSearchTarget
                txtSearchProcessName.Text = startSearchTarget;
                loadedSettings += "\nLoaded setting - Process: " + startSearchTarget;
            }   

            // StartInPausedMode
            if (startPaused)
            {
                btnPause.Content = iconPaused;
                btnPause.ToolTip = toolTipPaused;
                isPaused = true;
            }
            UpdateStatusLabel(startPaused ? "Paused" : "Waiting");
            loadedSettings += "\nLoaded setting - PausedMode: " + startPaused;

            // StartInPinnedMode
            if (startPinned)
            {
                btnToggleTopMost.Content = iconPinned;
                btnToggleTopMost.ToolTip = toolTipPinned;
                isPinned = true;
                this.Topmost = true;
            }
            loadedSettings += "\nLoaded setting - PinnedMode: " + startPinned;

            // TimerPollingRate Log
            loadedSettings += "\nLoaded setting - TimerInterval: " + startPollingRate;

            // Finished loading settings
            LogToConsole(loadedSettings, true);

            // Launch timer last
            timer = new Timer(startPollingRate);
            timer.Elapsed += Timer_Elapsed;
            if (!startPaused)
                timer.Start();

            LogToConsole("Timer initialized" + (timer.Enabled ? " and started" : "") + " with interval of " + timer.Interval + "ms", true);

            // Background timer to auto-save config
            // Set currently to 10 minute intervals
            fileTimer = new Timer();
            fileTimer.Interval = TimeSpan.FromMinutes(10).TotalMilliseconds;
            fileTimer.Elapsed += FileTimer_Elapsed;
            fileTimer.Start();

            startUp = false;
        }

        private void FileTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!hasSaved)
            {
                LogToConsole("Auto-saving config", true);
                Dispatcher.Invoke(() =>
                {
                    SaveConfig();
                });

                hasSaved = true;
            }
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
                    bool capturedProcessTarget = false;
                    if (isAltSearch)
                    {
                        string[] tempStrings = GetSearchProcessName().Trim().Split('.');
                        if (capturedProcess.ProcessName.Equals(tempStrings[0]))
                            capturedProcessTarget = true;
                    }
                    _ = NativeMethods.GetWindowRect(hWnd, out AltRect fgBounds);
                    AltRect scrnBounds = System.Windows.Forms.Screen.FromHandle(hWnd).Bounds;
                    if ((fgBounds.Bottom - fgBounds.Top) == scrnBounds.Height &&
                        (fgBounds.Right - fgBounds.Left) == scrnBounds.Width &&
                        (!isAltSearch || capturedProcessTarget))
                    {
                        if (!isMouseCaptured)
                        {
                            isMouseCaptured = true;
                            if (!isMuted)
                            {
                                NativeMethods.Beep(500, 20);
                                NativeMethods.Beep(700, 20);
                            }
                            LogToConsole("Captured mouse cursor to\nprocess: " + capturedProcess.ProcessName + "\ntitle: " + capturedProcess.MainWindowTitle);
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
            if (hasSaved && autoSaveOnExit)
                hasSaved = false;

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
            if (hasSaved && autoSaveOnExit)
                hasSaved = false;

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
                timer.Stop();
                fileTimer.Stop();
                btnPause.Content = iconPaused;
                btnPause.ToolTip = toolTipPaused;
                UpdateStatusLabel("Paused");
                LogToConsole("Paused, no longer watching for window");
            }

            // Options time
            OptionsWindow optionsWindow = new OptionsWindow(configFile)
            {
                ShowInTaskbar = false,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if ((bool)optionsWindow.ShowDialog())
            {
                ReadSettings();
                LogToConsole($"Changed timer interval from {timer.Interval.ToString(CultureInfo.CurrentCulture)}ms to {startPollingRate.ToString(CultureInfo.CurrentCulture)}ms");
                timer.Interval = startPollingRate;

                if (hasSaved)
                    hasSaved = false;
            }

            // Stop temp pause if enabled
            if (optionsPause)
            {
                timer.Start();
                fileTimer.Start();
                btnPause.Content = iconUnpaused;
                btnPause.ToolTip = toolTipUnpaused;
                UpdateStatusLabel("Waiting");
                LogToConsole("Unpaused, waiting for window focus");
            }
        }

        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            if (hasSaved && autoSaveOnExit)
                hasSaved = false;

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
            configFile.PausedMode = isPaused;

            configFile.ProcessSearchMode = isAltSearch;
            configFile.ExtendedMode = isExtended;
            configFile.MutedMode = isMuted;
            configFile.PinnedMode = isPinned;
            configFile.SaveProcessName = rememberSearchTarget;
        }

        private void SaveConfig()
        {
            if (autoSaveOnExit)
                UpdateAutoSavedSettings();

            if (rememberSearchTarget)
                configFile.Process = txtSearchProcessName.Text;

            configFile.WindowPosition = this.GetPlacement();
            WriteConfigFile(configFileLocation);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            timer.Stop();
            fileTimer.Stop();

            if (!hasSaved)
                SaveConfig();
            
            NativeMethods.ClipCursor(IntPtr.Zero);
            Dispose();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            Environment.Exit(0);
        }

        private void RdbSearchMode_Checked(object sender, RoutedEventArgs e)
        {
            if (hasSaved && autoSaveOnExit && !startUp)
                hasSaved = false;

            isAltSearch = (bool)rdbProcess.IsChecked;
            ToggleSearchProcessControls(isAltSearch);
        }

        private void BtnToggleTopMost_Click(object sender, RoutedEventArgs e)
        {
            if (hasSaved && autoSaveOnExit)
                hasSaved = false;
            
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
            // Hold something, we're going crazy
            Run run1 = new Run($"Version: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion}\n\nGitHub page:\n");
            Run run2 = new Run("\n\nBased on:\n");
            Run run3 = new Run("\n\nIcons by:\n");
            Run run4 = new Run(" (Buttons)\n");
            Run run5 = new Run(" (Window / Taskbar)");
            Run hyperRun1 = new Run("https://github.com/Triction/FullscreenLockConv");
            Run hyperRun2 = new Run("https://github.com/blaberry/FullscreenLock");
            Run hyperRun3 = new Run("https://materialdesignicons.com");
            Run hyperRun4 = new Run("https://icons8.com");
            Hyperlink hyperlink1 = new Hyperlink(hyperRun1)
            {
                NavigateUri = new Uri(hyperRun1.Text)
            };
            hyperlink1.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate);
            Hyperlink hyperlink2 = new Hyperlink(hyperRun2)
            {
                NavigateUri = new Uri(hyperRun2.Text)
            };
            hyperlink2.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate);
            Hyperlink hyperlink3 = new Hyperlink(hyperRun3)
            {
                NavigateUri = new Uri(hyperRun3.Text)
            };
            hyperlink3.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate);
            Hyperlink hyperlink4 = new Hyperlink(hyperRun4)
            {
                NavigateUri = new Uri(hyperRun4.Text)
            };
            hyperlink4.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate);
            List<Inline> inlines = new List<Inline>()
            {
                run1,
                hyperlink1,
                run2,
                hyperlink2,
                run3,
                hyperlink3,
                run4,
                hyperlink4,
                run5
            };
            string caption = "About";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage image = MessageBoxImage.Information;
            CustomMessageBox.ShowSpecial(this, inlines, caption, button, image);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void txtSearchProcessName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (hasSaved && autoSaveOnExit && !startUp)
                hasSaved = false;
        }
    }
}
