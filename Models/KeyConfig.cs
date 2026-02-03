using System.Windows.Forms;

namespace AutoKeyPresser.Models
{
    public class KeyConfig
    {
        public Keys Key { get; set; }
        public int DelayMs { get; set; }
        public System.Windows.Forms.Timer Timer { get; set; } = null!;
        public bool IsEnabled { get; set; } = true;
        public int PressCount { get; set; } = 0;

        public KeyConfig(Keys key, int delayMs)
        {
            Key = key;
            DelayMs = delayMs;
        }
    }
}
