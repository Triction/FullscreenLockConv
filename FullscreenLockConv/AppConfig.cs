using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FullscreenLockConv
{
    // IDEAS: Dictionary to store all config options so then we can handle all loading and saving from within the class itself.

    public class AppConfig
    {
        [JsonIgnore]
        public string ConfigDirectory { get; set; }
        [JsonIgnore]
        public string ConfigFile { get; set; }

        // Configuration values
        public bool AutoSaveLastUsedOptions { get; set; } = true;
        public bool RememberSearchTarget { get; set; } = true;
        public bool StartInExtendedMode { get; set; } = false;
        public bool StartInMutedMode { get; set; } = false;
        public bool StartInPausedMode { get; set; } = false;
        public bool StartInPinnedMode { get; set; } = false;
        public bool StartInProcessSearchMode { get; set; } = false;
        public double TimerPollingRate { get; set; } = 500d;
        public string LastKnownSearchTarget { get; set; }
        public string MainWindowPlacement { get; set; }

        public AppConfig(string configFile, string configDirectory)
        {
            ConfigFile = configFile;
            ConfigDirectory = configDirectory;
        }

        public AppConfig(string configFile) : this(configFile, Directory.GetCurrentDirectory())
        {
        }

        public string GetFullPath()
        {
            return Path.GetFullPath(ConfigDirectory + ConfigFile);
        }
    }
}
