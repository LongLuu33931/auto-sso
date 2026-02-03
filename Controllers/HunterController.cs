using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp.Extensions;
using AutoKeyPresser.Services;
using AutoKeyPresser.UI;

namespace AutoKeyPresser.Controllers
{
    /// <summary>
    /// Controller for Hunter functionality
    /// </summary>
    public class HunterController
    {
        private readonly HunterService _hunterService;
        private readonly HunterTabBuilder _ui;
        private readonly Action _toggleAutoKey;
        private readonly Func<bool> _isAutoKeyRunning;

        private bool _waitingForAttackKey = false;

        public bool IsWaitingForInput => _waitingForAttackKey;

        public HunterController(
            HunterService hunterService, 
            HunterTabBuilder ui,
            Action toggleAutoKey,
            Func<bool> isAutoKeyRunning)
        {
            _hunterService = hunterService;
            _ui = ui;
            _toggleAutoKey = toggleAutoKey;
            _isAutoKeyRunning = isAutoKeyRunning;

            WireUpEvents();
            SubscribeToService();
        }

        private void WireUpEvents()
        {
            _ui.OnLoadTemplateClick += (s, e) => LoadTemplate();
            _ui.OnCaptureClick += (s, e) => CaptureTemplate();
            _ui.OnSetAttackKeyClick += (s, e) => StartAttackKeySelection();
            _ui.OnStartHunterClick += (s, e) => Toggle();
        }

        private void SubscribeToService()
        {
            _hunterService.OnStatusChanged += status =>
            {
                if (_ui.LblHunterStatus.InvokeRequired)
                    _ui.LblHunterStatus.Invoke((MethodInvoker)(() => _ui.LblHunterStatus.Text = status));
                else
                    _ui.LblHunterStatus.Text = status;
            };

            _hunterService.OnRunningChanged += UpdateUI;
        }

        public void HandleKeyDown(Keys keyCode)
        {
            if (_waitingForAttackKey)
            {
                _waitingForAttackKey = false;
                _hunterService.AttackKey = keyCode;
                _ui.BtnSetAttackKey.Text = keyCode.ToString();
                _ui.BtnSetAttackKey.BackColor = Color.FromArgb(60, 60, 80);
            }
        }

        private void StartAttackKeySelection()
        {
            _waitingForAttackKey = true;
            _ui.BtnSetAttackKey.Text = "...";
            _ui.BtnSetAttackKey.BackColor = Color.Orange;
        }

        private void LoadTemplate()
        {
            using var ofd = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.bmp" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var bmp = new Bitmap(ofd.FileName);
                    _ui.PbTemplate.Image = bmp;
                    _hunterService.SetTemplate(BitmapConverter.ToMat(bmp));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void CaptureTemplate()
        {
            MessageBox.Show("Sau khi nhấn OK, bạn có 3 giây để chuyển sang game.\nSau đó kéo chuột để chọn vùng quái.", "Thông báo");
            Thread.Sleep(3000);

            using var snip = new Form
            {
                WindowState = FormWindowState.Maximized,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.5,
                Cursor = Cursors.Cross,
                TopMost = true
            };

            int startX = 0, startY = 0;
            bool drawing = false;
            Rectangle rect = Rectangle.Empty;

            snip.MouseDown += (s, me) => { startX = me.X; startY = me.Y; drawing = true; };
            snip.MouseMove += (s, me) =>
            {
                if (!drawing) return;
                rect = new Rectangle(
                    Math.Min(startX, me.X),
                    Math.Min(startY, me.Y),
                    Math.Abs(me.X - startX),
                    Math.Abs(me.Y - startY));
                snip.Invalidate();
            };
            snip.Paint += (s, pe) =>
            {
                if (rect != Rectangle.Empty)
                    pe.Graphics.DrawRectangle(Pens.Red, rect);
            };
            snip.MouseUp += (s, me) => { drawing = false; snip.Close(); };

            snip.ShowDialog();

            if (rect.Width > 5 && rect.Height > 5)
            {
                var bmp = new Bitmap(rect.Width, rect.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
                }
                _ui.PbTemplate.Image = bmp;
                _hunterService.SetTemplate(BitmapConverter.ToMat(bmp));
            }
        }

        public void Toggle()
        {
            if (_hunterService.IsRunning)
                Stop();
            else
                Start();
        }

        private void Start()
        {
            if (!_hunterService.HasTemplate)
            {
                MessageBox.Show("Vui lòng chọn mẫu quái trước!");
                return;
            }

            // Update settings from UI
            _hunterService.AttackDistance = (int)_ui.NumAttackDist.Value;
            _hunterService.MatchThreshold = (double)_ui.NumThreshold.Value / 100.0;
            _hunterService.YBiasRange = (int)_ui.NumYBias.Value;

            // Sync with Auto Key if enabled
            if (_ui.ChkSyncAutoKey.Checked && !_isAutoKeyRunning())
            {
                _toggleAutoKey();
            }

            _hunterService.Start();
        }

        private void Stop()
        {
            _hunterService.Stop();

            // Sync with Auto Key if enabled
            if (_ui.ChkSyncAutoKey.Checked && _isAutoKeyRunning())
            {
                _toggleAutoKey();
            }
        }

        private void UpdateUI(bool running)
        {
            if (_ui.BtnStartHunter.InvokeRequired)
            {
                _ui.BtnStartHunter.Invoke((MethodInvoker)(() => UpdateUI(running)));
                return;
            }

            if (running)
            {
                _ui.BtnStartHunter.Text = "⏹ DỪNG SĂN (F7)";
                _ui.BtnStartHunter.BackColor = Color.Red;
                _ui.LblHunterStatus.Text = "Trạng thái: 🟢 Đang săn...";
                _ui.LblHunterStatus.ForeColor = Color.Green;
            }
            else
            {
                _ui.BtnStartHunter.Text = "🏹 BẮT ĐẦU SĂN (F7)";
                _ui.BtnStartHunter.BackColor = Color.FromArgb(200, 100, 50);
                _ui.LblHunterStatus.Text = "Trạng thái: ⏸ Đã dừng";
                _ui.LblHunterStatus.ForeColor = Color.Gray;
            }
        }

        // For config
        public void ApplyConfig(int attackDistance, string attackKey, double matchThreshold, bool syncAutoKey, int yBiasRange)
        {
            _ui.NumAttackDist.Value = Math.Clamp(attackDistance, 10, 500);
            if (Enum.TryParse<Keys>(attackKey, out Keys k))
            {
                _hunterService.AttackKey = k;
                _ui.BtnSetAttackKey.Text = k.ToString();
            }
            _ui.NumThreshold.Value = Math.Clamp((decimal)(matchThreshold * 100), 50, 100);
            _ui.ChkSyncAutoKey.Checked = syncAutoKey;
            _ui.NumYBias.Value = Math.Clamp(yBiasRange, 10, 200);
        }

        public (int attackDistance, string attackKey, double matchThreshold, bool syncAutoKey, int yBiasRange) GetConfig()
        {
            return (
                (int)_ui.NumAttackDist.Value,
                _hunterService.AttackKey.ToString(),
                (double)_ui.NumThreshold.Value / 100.0,
                _ui.ChkSyncAutoKey.Checked,
                (int)_ui.NumYBias.Value
            );
        }
    }
}
