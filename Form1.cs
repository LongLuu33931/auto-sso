using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AutoKeyPresser.Controllers;
using AutoKeyPresser.Models;
using AutoKeyPresser.NativeApi;
using AutoKeyPresser.Services;
using AutoKeyPresser.UI;

// Fix ambiguity
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace AutoKeyPresser
{
    public partial class Form1 : Form
    {
        private const int HOTKEY_ID = 1;
        private const int HUNTER_HOTKEY_ID = 2;

        // Services
        private readonly KeySender _keySender;
        private readonly HunterService _hunterService;
        private readonly ConfigService _configService;

        // UI Builders
        private readonly AutoKeyTabBuilder _autoKeyTabBuilder;
        private readonly HunterTabBuilder _hunterTabBuilder;

        // Controllers
        private AutoKeyController _autoKeyController = null!;
        private HunterController _hunterController = null!;

        // UI
        private Label lblActiveWindow = null!;

        public Form1()
        {
            InitializeComponent();

            // Initialize services
            _keySender = new KeySender();
            _hunterService = new HunterService(_keySender);
            _configService = new ConfigService();

            // Initialize UI builders
            _autoKeyTabBuilder = new AutoKeyTabBuilder();
            _hunterTabBuilder = new HunterTabBuilder();

            // Build UI
            InitializeCustomComponents();

            // Initialize controllers (after UI is built)
            _autoKeyController = new AutoKeyController(_keySender, _autoKeyTabBuilder);
            _hunterController = new HunterController(
                _hunterService,
                _hunterTabBuilder,
                () => _autoKeyController.Toggle(),
                () => _autoKeyController.IsRunning
            );

            // Wire save button
            _autoKeyTabBuilder.OnSaveClick += (s, e) => SaveConfig();

            // Register hotkeys
            Win32.RegisterHotKey(this.Handle, HOTKEY_ID, 0, (uint)Keys.F6);
            Win32.RegisterHotKey(this.Handle, HUNTER_HOTKEY_ID, 0, (uint)Keys.F7);

            // Start window title monitor
            var windowTimer = new System.Windows.Forms.Timer { Interval = 300 };
            windowTimer.Tick += (s, e) => UpdateActiveWindow();
            windowTimer.Start();

            // Load saved config
            LoadConfig();
        }

        private void InitializeCustomComponents()
        {
            // Form setup
            this.Text = "Auto Key - Ghost Online Hunter v2";
            this.Size = new Size(580, 780);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(25, 25, 35);
            this.ForeColor = Color.White;
            this.KeyPreview = true;
            this.TopMost = true;

            // Title
            this.Controls.Add(new Label
            {
                Text = "🎮 Ghost Online Pro",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 180, 50),
                AutoSize = true,
                Location = new Point(160, 10)
            });

            // Active window label
            lblActiveWindow = new Label
            {
                Text = "🎯 Target: ---",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                AutoSize = true,
                Location = new Point(20, 45)
            };
            this.Controls.Add(lblActiveWindow);

            // Create tabs
            var tabControl = new TabControl
            {
                Location = new Point(12, 70),
                Size = new Size(540, 650),
                Font = new Font("Segoe UI", 10)
            };

            var tabAutoKey = new TabPage("Auto Key (F6)") { BackColor = Color.FromArgb(30, 30, 40) };
            _autoKeyTabBuilder.Build(tabAutoKey);

            var tabHunter = new TabPage("Auto Hunt (F7)") { BackColor = Color.FromArgb(30, 30, 40) };
            _hunterTabBuilder.Build(tabHunter);

            tabControl.Controls.Add(tabAutoKey);
            tabControl.Controls.Add(tabHunter);
            this.Controls.Add(tabControl);

            // Key event
            this.KeyDown += Form1_KeyDown;
        }

        private void UpdateActiveWindow()
        {
            IntPtr hwnd = Win32.GetForegroundWindow();
            var sb = new StringBuilder(256);
            Win32.GetWindowText(hwnd, sb, 256);
            string title = sb.ToString();

            string displayTitle = title.Length > 35 ? title.Substring(0, 32) + "..." : title;
            lblActiveWindow.Text = $"🎯 Target: {displayTitle}";

            lblActiveWindow.ForeColor = title.ToLower().Contains("ghost") || title.ToLower().Contains("online")
                ? Color.FromArgb(100, 255, 100)
                : Color.FromArgb(100, 200, 255);
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_autoKeyController.IsWaitingForInput)
            {
                _autoKeyController.HandleKeyDown(e.KeyCode);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (_hunterController.IsWaitingForInput)
            {
                _hunterController.HandleKeyDown(e.KeyCode);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID)
                    _autoKeyController.Toggle();
                else if (id == HUNTER_HOTKEY_ID)
                    _hunterController.Toggle();
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveConfig();
            Win32.UnregisterHotKey(this.Handle, HOTKEY_ID);
            Win32.UnregisterHotKey(this.Handle, HUNTER_HOTKEY_ID);
            base.OnFormClosing(e);
        }

        #region Config

        private void SaveConfig()
        {
            try
            {
                var (sendMethod, keyHoldTime, attachThread, defaultDelay, backgroundMode, targetWindowTitle) = _autoKeyController.GetConfig();
                var (attackDistance, attackKey, matchThreshold, syncAutoKey, yBiasRange) = _hunterController.GetConfig();

                var config = new SavedConfig
                {
                    SendMethod = sendMethod,
                    KeyHoldTime = keyHoldTime,
                    AttachThread = attachThread,
                    DefaultDelay = defaultDelay,
                    BackgroundMode = backgroundMode,
                    TargetWindowTitle = targetWindowTitle,
                    AttackDistance = attackDistance,
                    AttackKey = attackKey,
                    MatchThreshold = matchThreshold,
                    SyncAutoKey = syncAutoKey,
                    YBiasRange = yBiasRange
                };

                foreach (var kc in _autoKeyController.KeyConfigs)
                {
                    config.Keys.Add(new KeyData
                    {
                        KeyName = kc.Key.ToString(),
                        Delay = kc.DelayMs,
                        IsEnabled = kc.IsEnabled
                    });
                }

                _configService.Save(config);
                _autoKeyTabBuilder.LblStatus.Text = "💾 Đã lưu cấu hình!";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Error: " + ex.Message);
            }
        }

        private void LoadConfig()
        {
            try
            {
                var config = _configService.Load();
                if (config == null) return;

                _autoKeyController.ApplyConfig(
                    config.SendMethod,
                    config.KeyHoldTime,
                    config.AttachThread,
                    config.DefaultDelay
                );

                _autoKeyController.ApplyBackgroundConfig(
                    config.BackgroundMode,
                    config.TargetWindowTitle
                );

                _hunterController.ApplyConfig(
                    config.AttackDistance,
                    config.AttackKey,
                    config.MatchThreshold,
                    config.SyncAutoKey,
                    config.YBiasRange
                );

                foreach (var kd in config.Keys)
                {
                    if (Enum.TryParse<Keys>(kd.KeyName, out Keys key))
                        _autoKeyController.AddKeyToListInternal(key, kd.Delay, kd.IsEnabled);
                }
            }
            catch { }
        }

        #endregion
    }
}
