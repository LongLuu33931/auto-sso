using System;
using System.Drawing;
using System.Windows.Forms;
using AutoKeyPresser.Services;

namespace AutoKeyPresser.UI
{
    /// <summary>
    /// Builder class for Auto Key tab UI
    /// </summary>
    public class AutoKeyTabBuilder
    {
        // Output controls (will be set after Build)
        public ListView KeyListView { get; private set; } = null!;
        public NumericUpDown NumDelay { get; private set; } = null!;
        public NumericUpDown NumKeyHoldTime { get; private set; } = null!;
        public Button BtnStartStop { get; private set; } = null!;
        public Label LblStatus { get; private set; } = null!;
        public ComboBox CboSendMethod { get; private set; } = null!;
        public CheckBox ChkAttachThread { get; private set; } = null!;
        public Button BtnAddKey { get; private set; } = null!;
        public Button BtnSelectKey { get; private set; } = null!;
        
        // Background mode controls
        public CheckBox ChkBackgroundMode { get; private set; } = null!;
        public ComboBox CboTargetWindow { get; private set; } = null!;
        public Button BtnRefreshWindows { get; private set; } = null!;
        public Button BtnCaptureWindow { get; private set; } = null!;
        public Label LblTargetWindow { get; private set; } = null!;

        // Events
        public event EventHandler? OnSelectKeyClick;
        public event EventHandler? OnAddKeyClick;
        public event EventHandler? OnQuickKeyClick;
        public event EventHandler? OnRemoveClick;
        public event EventHandler? OnClearAllClick;
        public event EventHandler? OnEditClick;
        public event EventHandler? OnTestAllClick;
        public event EventHandler? OnStartStopClick;
        public event EventHandler? OnSaveClick;
        public event EventHandler? OnListViewDoubleClick;
        public event ItemCheckedEventHandler? OnItemChecked;
        public event EventHandler? OnSendMethodChanged;
        public event EventHandler? OnKeyHoldTimeChanged;
        public event EventHandler? OnAttachThreadChanged;
        
        // Background mode events
        public event EventHandler? OnBackgroundModeChanged;
        public event EventHandler? OnTargetWindowChanged;
        public event EventHandler? OnRefreshWindowsClick;
        public event EventHandler? OnCaptureWindowClick;

        public void Build(TabPage tab)
        {
            BuildBackgroundModeGroup(tab);
            BuildSettingsGroup(tab);
            BuildAddKeyGroup(tab);
            BuildQuickAddGroups(tab);
            BuildKeyListGroup(tab);
            BuildStartButton(tab);
        }

        private void BuildBackgroundModeGroup(TabPage tab)
        {
            var bgGroup = new GroupBox
            {
                Text = "🖥️ Chế độ Background (Không cần focus game)",
                ForeColor = Color.FromArgb(100, 255, 150),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(15, 10),
                Size = new Size(505, 85)
            };

            ChkBackgroundMode = new CheckBox
            {
                Text = "Bật Background Mode",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 100),
                AutoSize = true,
                Location = new Point(15, 25),
                Checked = false
            };
            ChkBackgroundMode.CheckedChanged += (s, e) => OnBackgroundModeChanged?.Invoke(s, e);

            BtnCaptureWindow = CreateButton("🎯 Bắt cửa sổ hiện tại", 200, 22, 150, 26, Color.FromArgb(180, 100, 50));
            BtnCaptureWindow.Click += (s, e) => OnCaptureWindowClick?.Invoke(s, e);

            BtnRefreshWindows = CreateButton("🔄", 355, 22, 35, 26, Color.FromArgb(60, 60, 80));
            BtnRefreshWindows.Click += (s, e) => OnRefreshWindowsClick?.Invoke(s, e);

            CboTargetWindow = new ComboBox
            {
                Location = new Point(15, 52),
                Size = new Size(375, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(50, 50, 65),
                ForeColor = Color.White
            };
            CboTargetWindow.SelectedIndexChanged += (s, e) => OnTargetWindowChanged?.Invoke(s, e);

            LblTargetWindow = new Label
            {
                Text = "(Chưa chọn cửa sổ)",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(395, 55)
            };

            bgGroup.Controls.AddRange(new Control[] { 
                ChkBackgroundMode, BtnCaptureWindow, BtnRefreshWindows, 
                CboTargetWindow, LblTargetWindow 
            });
            tab.Controls.Add(bgGroup);
        }

        private void BuildSettingsGroup(TabPage tab)
        {
            var methodGroup = new GroupBox
            {
                Text = "⚙️ Cài đặt chung",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 100),
                Size = new Size(505, 55)
            };

            CboSendMethod = new ComboBox
            {
                Location = new Point(15, 25),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 65),
                ForeColor = Color.White
            };
            CboSendMethod.Items.AddRange(new string[] {
                "SendInput Extended", "keybd_event", "PostMessage (Background)", "SendMessage (Background)", "Combined - All ⭐"
            });
            CboSendMethod.SelectedIndex = 4; // Default: Combined - All
            CboSendMethod.SelectedIndexChanged += (s, e) => OnSendMethodChanged?.Invoke(s, e);

            var lblHold = new Label { Text = "Hold:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(310, 28) };

            NumKeyHoldTime = new NumericUpDown
            {
                Minimum = 10, Maximum = 500, Value = 50, Increment = 10,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 65), ForeColor = Color.White,
                Location = new Point(350, 24), Size = new Size(60, 25)
            };
            NumKeyHoldTime.ValueChanged += (s, e) => OnKeyHoldTimeChanged?.Invoke(s, e);

            var lblHoldMs = new Label { Text = "ms", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true, Location = new Point(415, 28) };

            ChkAttachThread = new CheckBox
            {
                Text = "Attach Thread",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 200, 100),
                AutoSize = true, Location = new Point(310, 28), Checked = true
            };
            ChkAttachThread.CheckedChanged += (s, e) => OnAttachThreadChanged?.Invoke(s, e);

            var btnSave = CreateButton("💾 Lưu", 430, 22, 60, 26, Color.FromArgb(80, 120, 80));
            btnSave.Click += (s, e) => OnSaveClick?.Invoke(s, e);

            methodGroup.Controls.AddRange(new Control[] { CboSendMethod, NumKeyHoldTime, lblHoldMs, ChkAttachThread, btnSave });
            tab.Controls.Add(methodGroup);
        }

        private void BuildAddKeyGroup(TabPage tab)
        {
            var addKeyGroup = new GroupBox
            {
                Text = "➕ Thêm / Sửa phím",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 160),
                Size = new Size(505, 70)
            };

            BtnSelectKey = new Button
            {
                Text = "🎯 Chọn phím",
                Font = new Font("Segoe UI", 10),
                Size = new Size(130, 32), Location = new Point(15, 25),
                BackColor = Color.FromArgb(60, 60, 80), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            BtnSelectKey.Click += (s, e) => OnSelectKeyClick?.Invoke(s, e);

            var lblDelay = new Label { Text = "Delay:", Font = new Font("Segoe UI", 10), AutoSize = true, Location = new Point(160, 30) };

            NumDelay = new NumericUpDown
            {
                Minimum = 100, Maximum = 60000, Value = 1000, Increment = 100,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(50, 50, 65), ForeColor = Color.White,
                Location = new Point(215, 25), Size = new Size(90, 30)
            };

            var lblMs = new Label { Text = "ms", Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, AutoSize = true, Location = new Point(310, 30) };

            BtnAddKey = new Button
            {
                Text = "➕ Thêm",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(100, 32), Location = new Point(390, 25),
                BackColor = Color.FromArgb(50, 150, 100), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            BtnAddKey.FlatAppearance.BorderSize = 0;
            BtnAddKey.Click += (s, e) => OnAddKeyClick?.Invoke(s, e);

            addKeyGroup.Controls.AddRange(new Control[] { BtnSelectKey, lblDelay, NumDelay, lblMs, BtnAddKey });
            tab.Controls.Add(addKeyGroup);
        }

        private void BuildQuickAddGroups(TabPage tab)
        {
            // Quick Add Group 1 - Letter keys
            var quickAddGroup1 = new GroupBox
            {
                Text = "⚡ Phím nhanh",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 235),
                Size = new Size(505, 50)
            };
            
            string[] letterKeys = { "Z", "X", "C", "A", "S", "D", "Q", "E", "W", "Space" };
            int xPos = 8;
            foreach (var keyName in letterKeys)
            {
                var btn = CreateQuickButton(keyName == "Space" ? "␣" : keyName, xPos, keyName, Color.FromArgb(70, 70, 90));
                btn.Click += (s, e) => OnQuickKeyClick?.Invoke(s, e);
                quickAddGroup1.Controls.Add(btn);
                xPos += 49;
            }
            tab.Controls.Add(quickAddGroup1);

            // Quick Add Group 2 - Arrow & number keys
            var quickAddGroup2 = new GroupBox
            {
                Text = "⬆️ Mũi tên & Số",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 290),
                Size = new Size(505, 50)
            };
            
            string[] arrowKeys = { "Up", "Down", "Left", "Right", "D1", "D2", "D3", "D4", "F1", "F2" };
            string[] arrowLabels = { "↑", "↓", "←", "→", "1", "2", "3", "4", "F1", "F2" };
            xPos = 8;
            for (int i = 0; i < arrowKeys.Length; i++)
            {
                var btn = CreateQuickButton(arrowLabels[i], xPos, arrowKeys[i], Color.FromArgb(90, 70, 90));
                btn.Click += (s, e) => OnQuickKeyClick?.Invoke(s, e);
                quickAddGroup2.Controls.Add(btn);
                xPos += 49;
            }
            tab.Controls.Add(quickAddGroup2);
        }

        private void BuildKeyListGroup(TabPage tab)
        {
            var listGroup = new GroupBox
            {
                Text = "📋 Danh sách phím (Click đúp để sửa)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 345),
                Size = new Size(505, 150)
            };

            KeyListView = new ListView
            {
                Location = new Point(15, 22),
                Size = new Size(380, 115),
                View = View.Details,
                FullRowSelect = true, GridLines = true,
                BackColor = Color.FromArgb(40, 40, 55), ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None, CheckBoxes = true
            };
            KeyListView.Columns.Add("Phím", 80, HorizontalAlignment.Center);
            KeyListView.Columns.Add("Delay", 70, HorizontalAlignment.Center);
            KeyListView.Columns.Add("Status", 100, HorizontalAlignment.Center);
            KeyListView.Columns.Add("Count", 80, HorizontalAlignment.Center);
            KeyListView.DoubleClick += (s, e) => OnListViewDoubleClick?.Invoke(s, e);
            KeyListView.ItemChecked += (s, e) => OnItemChecked?.Invoke(s, e);

            var btnRemove = CreateButton("🗑️ Xóa", 405, 22, 95, 30, Color.FromArgb(180, 60, 60));
            btnRemove.Click += (s, e) => OnRemoveClick?.Invoke(s, e);
            
            var btnClear = CreateButton("🧹 Xóa hết", 405, 58, 95, 30, Color.FromArgb(120, 60, 60));
            btnClear.Click += (s, e) => OnClearAllClick?.Invoke(s, e);
            
            var btnEdit = CreateButton("✏️ Sửa", 405, 94, 95, 30, Color.FromArgb(200, 150, 50));
            btnEdit.Click += (s, e) => OnEditClick?.Invoke(s, e);
            
            var btnTestAll = CreateButton("🧪 Test All", 405, 130, 95, 30, Color.FromArgb(150, 80, 150));
            btnTestAll.Click += (s, e) => OnTestAllClick?.Invoke(s, e);

            listGroup.Controls.AddRange(new Control[] { KeyListView, btnRemove, btnClear, btnEdit, btnTestAll });
            tab.Controls.Add(listGroup);
        }

        private void BuildStartButton(TabPage tab)
        {
            BtnStartStop = new Button
            {
                Text = "▶ BẮT ĐẦU (F6)",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Size = new Size(505, 50), Location = new Point(15, 505),
                BackColor = Color.FromArgb(50, 180, 100), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            BtnStartStop.FlatAppearance.BorderSize = 0;
            BtnStartStop.Click += (s, e) => OnStartStopClick?.Invoke(s, e);
            tab.Controls.Add(BtnStartStop);

            LblStatus = new Label
            {
                Text = "⏸ Nhấn F6 để Start/Stop",
                Font = new Font("Segoe UI", 10), ForeColor = Color.Gray,
                AutoSize = true, Location = new Point(180, 565)
            };
            tab.Controls.Add(LblStatus);
        }

        private Button CreateButton(string text, int x, int y, int w, int h, Color color)
        {
            var btn = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, h),
                BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateQuickButton(string text, int x, string tag, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Size = new Size(44, 26), Location = new Point(x, 20),
                BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Tag = tag, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}
