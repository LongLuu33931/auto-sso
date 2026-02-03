using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoKeyPresser.UI
{
    /// <summary>
    /// Builder class for Hunter tab UI
    /// </summary>
    public class HunterTabBuilder
    {
        // Output controls
        public PictureBox PbTemplate { get; private set; } = null!;
        public Button BtnStartHunter { get; private set; } = null!;
        public Label LblHunterStatus { get; private set; } = null!;
        public NumericUpDown NumAttackDist { get; private set; } = null!;
        public Button BtnSetAttackKey { get; private set; } = null!;
        public NumericUpDown NumThreshold { get; private set; } = null!;
        public CheckBox ChkSyncAutoKey { get; private set; } = null!;
        public NumericUpDown NumYBias { get; private set; } = null!;

        // Events
        public event EventHandler? OnLoadTemplateClick;
        public event EventHandler? OnCaptureClick;
        public event EventHandler? OnSetAttackKeyClick;
        public event EventHandler? OnStartHunterClick;

        public void Build(TabPage tab)
        {
            BuildTemplateGroup(tab);
            BuildSettingsGroup(tab);
            BuildStatusAndStartButton(tab);
        }

        private void BuildTemplateGroup(TabPage tab)
        {
            var grpTemplate = new GroupBox
            {
                Text = "1. Mẫu quái (Hình ảnh)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 15),
                Size = new Size(505, 130)
            };

            PbTemplate = new PictureBox
            {
                Location = new Point(15, 25),
                Size = new Size(200, 95),
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnLoadTemplate = CreateButton("📂 Chọn ảnh", 230, 25, 120, 35, Color.FromArgb(60, 60, 80));
            btnLoadTemplate.Click += (s, e) => OnLoadTemplateClick?.Invoke(s, e);

            var btnCapture = CreateButton("📸 Cắt từ màn hình", 230, 70, 150, 35, Color.FromArgb(180, 100, 50));
            btnCapture.Click += (s, e) => OnCaptureClick?.Invoke(s, e);

            grpTemplate.Controls.AddRange(new Control[] { PbTemplate, btnLoadTemplate, btnCapture });
            tab.Controls.Add(grpTemplate);
        }

        private void BuildSettingsGroup(TabPage tab)
        {
            var grpSettings = new GroupBox
            {
                Text = "2. Cấu hình đánh",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 160),
                Size = new Size(505, 140)
            };

            // Row 1
            var lblDist = new Label { Text = "Khoảng đánh(px):", Location = new Point(20, 35), AutoSize = true };
            NumAttackDist = new NumericUpDown
            {
                Minimum = 10, Maximum = 500, Value = 60,
                Location = new Point(130, 32), Size = new Size(60, 25),
                BackColor = Color.FromArgb(50, 50, 65), ForeColor = Color.White
            };

            var lblKey = new Label { Text = "Phím đánh:", Location = new Point(230, 35), AutoSize = true };
            BtnSetAttackKey = CreateButton("Z", 310, 30, 60, 30, Color.FromArgb(60, 60, 80));
            BtnSetAttackKey.Click += (s, e) => OnSetAttackKeyClick?.Invoke(s, e);

            var lblThreshold = new Label { Text = "Độ tin cậy(%):", Location = new Point(380, 35), AutoSize = true };
            NumThreshold = new NumericUpDown
            {
                Minimum = 50, Maximum = 100, Value = 80,
                Location = new Point(465, 32), Size = new Size(35, 25),
                BackColor = Color.FromArgb(50, 50, 65), ForeColor = Color.White
            };

            // Row 2
            var lblYBias = new Label { Text = "Lệch trục Y tối đa:", Location = new Point(20, 75), AutoSize = true };
            NumYBias = new NumericUpDown
            {
                Minimum = 10, Maximum = 200, Value = 40,
                Location = new Point(130, 72), Size = new Size(60, 25),
                BackColor = Color.FromArgb(50, 50, 65), ForeColor = Color.White
            };
            var lblYInfo = new Label { Text = "(±40 pixel)", Location = new Point(195, 75), AutoSize = true, ForeColor = Color.Gray };

            ChkSyncAutoKey = new CheckBox
            {
                Text = "🔗 Kết hợp chạy cùng Auto Key",
                Location = new Point(230, 105), AutoSize = true,
                ForeColor = Color.FromArgb(100, 255, 150),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            grpSettings.Controls.AddRange(new Control[] {
                lblDist, NumAttackDist, lblKey, BtnSetAttackKey,
                lblThreshold, NumThreshold, lblYBias, NumYBias, lblYInfo, ChkSyncAutoKey
            });
            tab.Controls.Add(grpSettings);
        }

        private void BuildStatusAndStartButton(TabPage tab)
        {
            LblHunterStatus = new Label
            {
                Text = "Trạng thái: ⏸ Sẵn sàng",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Gray, AutoSize = true,
                Location = new Point(20, 310)
            };
            tab.Controls.Add(LblHunterStatus);

            BtnStartHunter = new Button
            {
                Text = "🏹 BẮT ĐẦU SĂN (F7)",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Size = new Size(505, 60), Location = new Point(15, 340),
                BackColor = Color.FromArgb(200, 100, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            BtnStartHunter.FlatAppearance.BorderSize = 0;
            BtnStartHunter.Click += (s, e) => OnStartHunterClick?.Invoke(s, e);
            tab.Controls.Add(BtnStartHunter);

            var lblWarn = new Label
            {
                Text = "⚠️ Yêu cầu: Game ở chế độ Cửa sổ (Windowed).\nƯu tiên đánh quái thẳng hàng ngang (Y ± 40px)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(255, 150, 100),
                AutoSize = true, Location = new Point(20, 420)
            };
            tab.Controls.Add(lblWarn);
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
    }
}
