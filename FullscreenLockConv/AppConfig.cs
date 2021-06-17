namespace FullscreenLockConv
{
    public class AppConfig
    {
        public bool AutoSaveConfig { get; set; }
        public bool ExtendedMode { get; set; }
        public bool MutedMode { get; set; }
        public bool PausedMode { get; set; }
        public bool PinnedMode { get; set; }
        public bool ProcessSearchMode { get; set; }
        public bool SaveProcessName { get; set; }
        public string Process { get; set; }
        public double TimerInterval { get; set; }
        public string WindowPosition { get; set; }

        public AppConfig()
        {
            AutoSaveConfig = true;
            ExtendedMode = false;
            MutedMode = false;
            PausedMode = false;
            PinnedMode = false;
            ProcessSearchMode = false;
            SaveProcessName = true;
            Process = "";
            TimerInterval = 500d;
            WindowPosition = "";
        }
    }
}
