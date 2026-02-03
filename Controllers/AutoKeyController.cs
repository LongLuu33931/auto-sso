using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AutoKeyPresser.Models;
using AutoKeyPresser.Services;
using AutoKeyPresser.UI;

namespace AutoKeyPresser.Controllers
{
    /// <summary>
    /// Controller for Auto Key functionality
    /// </summary>
    public class AutoKeyController
    {
        private readonly KeySender _keySender;
        private readonly AutoKeyTabBuilder _ui;
        private readonly List<KeyConfig> _keyConfigs = new List<KeyConfig>();
        
        private bool _isRunning = false;
        private bool _waitingForKeyInput = false;
        private bool _editingMode = false;
        private Keys _pendingKey = Keys.None;

        public bool IsRunning => _isRunning;
        public List<KeyConfig> KeyConfigs => _keyConfigs;

        public AutoKeyController(KeySender keySender, AutoKeyTabBuilder ui)
        {
            _keySender = keySender;
            _ui = ui;
            
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            _ui.OnSelectKeyClick += (s, e) => StartKeySelection();
            _ui.OnAddKeyClick += (s, e) => AddOrUpdateKey();
            _ui.OnQuickKeyClick += HandleQuickKeyClick;
            _ui.OnRemoveClick += (s, e) => RemoveSelectedKey();
            _ui.OnClearAllClick += (s, e) => ClearAllKeys();
            _ui.OnEditClick += (s, e) => EnterEditMode();
            _ui.OnTestAllClick += (s, e) => TestAllMethods();
            _ui.OnStartStopClick += (s, e) => Toggle();
            _ui.OnListViewDoubleClick += (s, e) => EnterEditMode();
            _ui.OnItemChecked += HandleItemChecked;
            _ui.OnSendMethodChanged += (s, e) => _keySender.CurrentMethod = (SendMethod)_ui.CboSendMethod.SelectedIndex;
            _ui.OnKeyHoldTimeChanged += (s, e) => _keySender.KeyHoldTimeMs = (int)_ui.NumKeyHoldTime.Value;
            _ui.OnAttachThreadChanged += (s, e) => _keySender.AttachThread = _ui.ChkAttachThread.Checked;
            
            // Background mode events
            _ui.OnBackgroundModeChanged += (s, e) => HandleBackgroundModeChanged();
            _ui.OnRefreshWindowsClick += (s, e) => RefreshWindowList();
            _ui.OnCaptureWindowClick += (s, e) => CaptureCurrentWindow();
            _ui.OnTargetWindowChanged += (s, e) => HandleTargetWindowChanged();
            
            // Initialize window list
            RefreshWindowList();
        }

        #region Background Mode

        private List<(IntPtr Handle, string Title)> _windowList = new();

        private void HandleBackgroundModeChanged()
        {
            _keySender.UseBackgroundMode = _ui.ChkBackgroundMode.Checked;
            
            if (_ui.ChkBackgroundMode.Checked)
            {
                _ui.LblTargetWindow.ForeColor = Color.FromArgb(100, 255, 150);
                _ui.LblStatus.Text = "🖥️ Background Mode - Bạn có thể làm việc khác!";
                
                // Auto-select PostMessage method for background
                if (_ui.CboSendMethod.SelectedIndex < 2)
                {
                    _ui.CboSendMethod.SelectedIndex = 2; // PostMessage
                }
            }
            else
            {
                _ui.LblTargetWindow.ForeColor = Color.Gray;
                _ui.LblStatus.Text = "⏸ Nhấn F6 để Start/Stop";
            }
        }

        private void RefreshWindowList()
        {
            _windowList = _keySender.GetAllWindows();
            _ui.CboTargetWindow.Items.Clear();
            
            foreach (var (handle, title) in _windowList)
            {
                string displayTitle = title.Length > 50 ? title.Substring(0, 47) + "..." : title;
                _ui.CboTargetWindow.Items.Add(displayTitle);
            }

            // Try to auto-select Ghost Online window
            for (int i = 0; i < _windowList.Count; i++)
            {
                if (_windowList[i].Title.ToLower().Contains("ghost") || 
                    _windowList[i].Title.ToLower().Contains("online"))
                {
                    _ui.CboTargetWindow.SelectedIndex = i;
                    break;
                }
            }
        }

        private void CaptureCurrentWindow()
        {
            _keySender.CaptureCurrentWindow();
            
            if (_keySender.TargetWindow != IntPtr.Zero)
            {
                // Refresh list and select the captured window
                RefreshWindowList();
                
                for (int i = 0; i < _windowList.Count; i++)
                {
                    if (_windowList[i].Handle == _keySender.TargetWindow)
                    {
                        _ui.CboTargetWindow.SelectedIndex = i;
                        break;
                    }
                }
                
                _ui.LblTargetWindow.Text = "✅ Đã bắt!";
                _ui.LblTargetWindow.ForeColor = Color.FromArgb(100, 255, 100);
                _ui.ChkBackgroundMode.Checked = true;
            }
        }

        private void HandleTargetWindowChanged()
        {
            int idx = _ui.CboTargetWindow.SelectedIndex;
            if (idx >= 0 && idx < _windowList.Count)
            {
                _keySender.SetTargetWindow(_windowList[idx].Handle);
                _ui.LblTargetWindow.Text = "✅";
                _ui.LblTargetWindow.ForeColor = Color.FromArgb(100, 255, 100);
            }
        }

        #endregion

        public void HandleKeyDown(Keys keyCode)
        {
            if (_waitingForKeyInput)
            {
                _waitingForKeyInput = false;
                _pendingKey = keyCode;
                _ui.BtnSelectKey.Text = $"✓ {_pendingKey}";
                _ui.BtnSelectKey.BackColor = Color.FromArgb(100, 180, 100);
            }
        }

        public bool IsWaitingForInput => _waitingForKeyInput;

        private void StartKeySelection()
        {
            _waitingForKeyInput = true;
            _pendingKey = Keys.None;
            _ui.BtnSelectKey.Text = "⌨️ Nhấn phím...";
            _ui.BtnSelectKey.BackColor = Color.FromArgb(255, 180, 50);
        }

        private void AddOrUpdateKey()
        {
            if (_pendingKey == Keys.None)
            {
                MessageBox.Show("Chọn phím trước!");
                return;
            }

            if (_editingMode)
            {
                UpdateSelectedKey();
                ExitEditMode();
            }
            else
            {
                AddKeyToList(_pendingKey, (int)_ui.NumDelay.Value);
                ResetKeySelection();
            }
        }

        private void UpdateSelectedKey()
        {
            if (_ui.KeyListView.SelectedItems.Count == 0) return;

            var item = _ui.KeyListView.SelectedItems[0];
            var config = (KeyConfig)item.Tag;

            config.Key = _pendingKey;
            config.DelayMs = (int)_ui.NumDelay.Value;
            config.Timer.Interval = config.DelayMs;

            item.SubItems[0].Text = GetDisplayKey(config.Key);
            item.SubItems[1].Text = $"{config.DelayMs}ms";
        }

        private void ExitEditMode()
        {
            _editingMode = false;
            _ui.BtnAddKey.Text = "➕ Thêm";
            _ui.BtnAddKey.BackColor = Color.FromArgb(50, 150, 100);
            ResetKeySelection();
        }

        private void ResetKeySelection()
        {
            _pendingKey = Keys.None;
            _ui.BtnSelectKey.Text = "🎯 Chọn phím";
            _ui.BtnSelectKey.BackColor = Color.FromArgb(60, 60, 80);
        }

        private void HandleQuickKeyClick(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string keyName)
            {
                Keys key = ParseKeyName(keyName);

                if (_editingMode)
                {
                    _pendingKey = key;
                    _ui.BtnSelectKey.Text = $"✓ {_pendingKey}";
                    return;
                }

                AddKeyToList(key, (int)_ui.NumDelay.Value);
            }
        }

        private void EnterEditMode()
        {
            if (_ui.KeyListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Chọn phím để sửa!", "Thông báo");
                return;
            }

            var item = _ui.KeyListView.SelectedItems[0];
            var config = (KeyConfig)item.Tag;

            _pendingKey = config.Key;
            _ui.BtnSelectKey.Text = $"✓ {_pendingKey}";
            _ui.BtnSelectKey.BackColor = Color.FromArgb(100, 180, 100);
            _ui.NumDelay.Value = config.DelayMs;

            _editingMode = true;
            _ui.BtnAddKey.Text = "💾 Lưu lại";
            _ui.BtnAddKey.BackColor = Color.FromArgb(200, 150, 50);
        }

        private void AddKeyToList(Keys key, int delay)
        {
            foreach (var config in _keyConfigs)
            {
                if (config.Key == key)
                {
                    MessageBox.Show("Đã tồn tại!");
                    return;
                }
            }
            AddKeyToListInternal(key, delay, true);
        }

        public void AddKeyToListInternal(Keys key, int delay, bool isEnabled)
        {
            var keyConfig = new KeyConfig(key, delay) { IsEnabled = isEnabled };
            keyConfig.Timer = new System.Windows.Forms.Timer { Interval = delay, Tag = keyConfig };
            keyConfig.Timer.Tick += KeyTimer_Tick;
            _keyConfigs.Add(keyConfig);

            var item = new ListViewItem(new string[] { GetDisplayKey(key), $"{delay}ms", "Ready", "0" });
            item.Tag = keyConfig;
            item.Checked = isEnabled;
            _ui.KeyListView.Items.Add(item);
        }

        private void RemoveSelectedKey()
        {
            if (_ui.KeyListView.SelectedItems.Count == 0) return;

            var item = _ui.KeyListView.SelectedItems[0];
            var config = (KeyConfig)item.Tag;
            config.Timer.Stop();
            config.Timer.Dispose();
            _keyConfigs.Remove(config);
            _ui.KeyListView.Items.Remove(item);

            if (_editingMode) ExitEditMode();
        }

        private void ClearAllKeys()
        {
            if (_keyConfigs.Count == 0) return;
            if (MessageBox.Show("Xóa hết?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            foreach (var c in _keyConfigs)
            {
                c.Timer.Stop();
                c.Timer.Dispose();
            }
            _keyConfigs.Clear();
            _ui.KeyListView.Items.Clear();
        }

        private void HandleItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag is KeyConfig c)
            {
                c.IsEnabled = e.Item.Checked;
                if (_isRunning)
                {
                    if (c.IsEnabled) c.Timer.Start();
                    else c.Timer.Stop();
                }
            }
        }

        private void TestAllMethods()
        {
            if (_ui.KeyListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Chọn phím để test!");
                return;
            }

            var keyConfig = (KeyConfig)_ui.KeyListView.SelectedItems[0].Tag;
            _ui.LblStatus.Text = "Testing in 2s...";
            _ui.LblStatus.Refresh();
            Thread.Sleep(2000);

            foreach (SendMethod m in Enum.GetValues(typeof(SendMethod)))
            {
                _keySender.CurrentMethod = m;
                _keySender.SendKey(keyConfig.Key);
                Thread.Sleep(300);
            }
            _keySender.CurrentMethod = (SendMethod)_ui.CboSendMethod.SelectedIndex;
            MessageBox.Show("Done testing");
        }

        public void Toggle()
        {
            if (_keyConfigs.Count == 0 && !_isRunning)
            {
                MessageBox.Show("Chưa có phím nào!");
                return;
            }

            _isRunning = !_isRunning;

            if (_isRunning)
            {
                foreach (var c in _keyConfigs)
                    if (c.IsEnabled) c.Timer.Start();
                _ui.BtnStartStop.Text = "⏹ DỪNG (F6)";
                _ui.BtnStartStop.BackColor = Color.FromArgb(220, 80, 80);
            }
            else
            {
                foreach (var c in _keyConfigs) c.Timer.Stop();
                _ui.BtnStartStop.Text = "▶ BẮT ĐẦU (F6)";
                _ui.BtnStartStop.BackColor = Color.FromArgb(50, 180, 100);
            }
        }

        private void KeyTimer_Tick(object? sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.Timer t && t.Tag is KeyConfig c)
            {
                _keySender.SendKey(c.Key);
                c.PressCount++;

                foreach (ListViewItem item in _ui.KeyListView.Items)
                {
                    if (item.Tag == c)
                    {
                        item.SubItems[3].Text = c.PressCount.ToString();
                        break;
                    }
                }
            }
        }

        private Keys ParseKeyName(string keyName)
        {
            return keyName switch
            {
                "Space" => Keys.Space,
                "Up" => Keys.Up,
                "Down" => Keys.Down,
                "Left" => Keys.Left,
                "Right" => Keys.Right,
                _ => (Keys)Enum.Parse(typeof(Keys), keyName)
            };
        }

        private string GetDisplayKey(Keys key)
        {
            return key switch
            {
                Keys.Up => "↑ Up",
                Keys.Down => "↓ Down",
                Keys.Left => "← Left",
                Keys.Right => "→ Right",
                _ => key.ToString()
            };
        }

        // For config loading
        public void ApplyConfig(int sendMethod, int keyHoldTime, bool attachThread, int defaultDelay)
        {
            _ui.CboSendMethod.SelectedIndex = Math.Clamp(sendMethod, 0, 4);
            _ui.NumKeyHoldTime.Value = Math.Clamp(keyHoldTime, 10, 500);
            _ui.ChkAttachThread.Checked = attachThread;
            _ui.NumDelay.Value = Math.Clamp(defaultDelay, 100, 60000);
        }

        public void ApplyBackgroundConfig(bool backgroundMode, string targetWindowTitle)
        {
            _ui.ChkBackgroundMode.Checked = backgroundMode;
            
            if (!string.IsNullOrEmpty(targetWindowTitle))
            {
                _keySender.FindAndSetTargetWindow(targetWindowTitle);
                RefreshWindowList();
                
                // Select matching window in combo
                for (int i = 0; i < _windowList.Count; i++)
                {
                    if (_windowList[i].Handle == _keySender.TargetWindow)
                    {
                        _ui.CboTargetWindow.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        public (int sendMethod, int keyHoldTime, bool attachThread, int defaultDelay, bool backgroundMode, string targetWindowTitle) GetConfig()
        {
            return (
                _ui.CboSendMethod.SelectedIndex,
                (int)_ui.NumKeyHoldTime.Value,
                _ui.ChkAttachThread.Checked,
                (int)_ui.NumDelay.Value,
                _ui.ChkBackgroundMode.Checked,
                _keySender.TargetWindowTitle
            );
        }
    }
}
