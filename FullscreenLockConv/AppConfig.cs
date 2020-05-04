namespace FullscreenLockConv
{
    public class AppConfig
    {
        public bool AutoSaveLastUsedOptions { get; set; }
        public bool RememberSearchTarget { get; set; }
        public bool StartInExtendedMode { get; set; }
        public bool StartInMutedMode { get; set; }
        public bool StartInPausedMode { get; set; }
        public bool StartInPinnedMode { get; set; }
        public bool StartInProcessSearchMode { get; set; }
        public double TimerPollingRate { get; set; }
        public string LastKnownSearchTarget { get; set; }
        public string MainWindowPlacement { get; set; }

        public AppConfig()
        {
            AutoSaveLastUsedOptions = true;
            RememberSearchTarget = true;
            StartInExtendedMode = false;
            StartInMutedMode = false;
            StartInPausedMode = false;
            StartInPinnedMode = false;
            StartInProcessSearchMode = false;
            TimerPollingRate = 500d;
            LastKnownSearchTarget = "";
            MainWindowPlacement = "";
        }
    }
}
