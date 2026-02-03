using System.Collections.Generic;

namespace AutoKeyPresser.Models
{
    /// <summary>
    /// Config class for JSON serialization
    /// </summary>
    public class SavedConfig
    {
        public List<KeyData> Keys { get; set; } = new List<KeyData>();
        public int SendMethod { get; set; } = 0;
        public int KeyHoldTime { get; set; } = 50;
        public bool AttachThread { get; set; } = true;
        public int DefaultDelay { get; set; } = 1000;

        // Background mode settings
        public bool BackgroundMode { get; set; } = false;
        public string TargetWindowTitle { get; set; } = "";

        // Auto Hunt settings
        public int AttackDistance { get; set; } = 60;
        public string AttackKey { get; set; } = "Z";
        public double MatchThreshold { get; set; } = 0.8;
        public bool SyncAutoKey { get; set; } = false;
        public int YBiasRange { get; set; } = 40;
    }

    public class KeyData
    {
        public string KeyName { get; set; } = "";
        public int Delay { get; set; } = 1000;
        public bool IsEnabled { get; set; } = true;
    }
}
