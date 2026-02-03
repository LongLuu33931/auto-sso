using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using AutoKeyPresser.NativeApi;

namespace AutoKeyPresser.Services
{
    /// <summary>
    /// Service for auto-hunting monsters using template matching
    /// </summary>
    public class HunterService
    {
        private readonly KeySender _keySender;
        private Mat? _templateImage;
        private CancellationTokenSource? _cts;

        public bool IsRunning { get; private set; }
        public Keys AttackKey { get; set; } = Keys.Z;
        public int AttackDistance { get; set; } = 60;
        public double MatchThreshold { get; set; } = 0.8;
        public int YBiasRange { get; set; } = 40;

        public event Action<string>? OnStatusChanged;
        public event Action<bool>? OnRunningChanged;

        public HunterService(KeySender keySender)
        {
            _keySender = keySender;
        }

        public void SetTemplate(Mat template)
        {
            _templateImage?.Dispose();
            _templateImage = template;
        }

        public bool HasTemplate => _templateImage != null;

        public void Start()
        {
            if (_templateImage == null)
            {
                throw new InvalidOperationException("Template image not set");
            }

            IsRunning = true;
            OnRunningChanged?.Invoke(true);
            _cts = new CancellationTokenSource();
            Task.Run(() => HunterLoop(_cts.Token));
        }

        public void Stop()
        {
            IsRunning = false;
            _cts?.Cancel();
            OnRunningChanged?.Invoke(false);
            OnStatusChanged?.Invoke("⏸ Đã dừng");
        }

        private async Task HunterLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IntPtr hwnd = Win32.GetForegroundWindow();
                    Win32.RECT rect;
                    if (Win32.GetWindowRect(hwnd, out rect))
                    {
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;

                        if (width > 100 && height > 100)
                        {
                            using (Bitmap screen = new Bitmap(width, height))
                            {
                                using (Graphics g = Graphics.FromImage(screen))
                                {
                                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, screen.Size);
                                }
                                ProcessGameScreen(screen, width, height);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Hunter Error: " + ex.Message);
                }

                await Task.Delay(100);
            }
        }

        private void ProcessGameScreen(Bitmap screen, int gameW, int gameH)
        {
            if (_templateImage == null) return;

            using (Mat screenMat = BitmapConverter.ToMat(screen))
            using (Mat res = new Mat())
            {
                Cv2.MatchTemplate(screenMat, _templateImage, res, TemplateMatchModes.CCoeffNormed);

                int charCenterX = gameW / 2;
                int charCenterY = gameH / 2;

                int bestX = -1;
                double bestMatchVal = 0;
                int minDistanceX = int.MaxValue;
                bool found = false;

                // Loop to find up to 10 candidates
                for (int i = 0; i < 10; i++)
                {
                    double minVal, maxVal;
                    OpenCvSharp.Point minLoc, maxLoc;
                    Cv2.MinMaxLoc(res, out minVal, out maxVal, out minLoc, out maxLoc);

                    if (maxVal < MatchThreshold) break;

                    int objCenterX = maxLoc.X + (_templateImage.Width / 2);
                    int objCenterY = maxLoc.Y + (_templateImage.Height / 2);

                    int yDiff = Math.Abs(objCenterY - charCenterY);

                    // Mask out this area so next iteration finds different monster
                    int rangeX = _templateImage.Width;
                    int rangeY = _templateImage.Height;
                    Cv2.Rectangle(res,
                        new Rect(maxLoc.X, maxLoc.Y, rangeX, rangeY),
                        new Scalar(0), -1);

                    // Logic: Check Y-Bias
                    if (yDiff <= YBiasRange)
                    {
                        // Valid Y position. Now check X distance.
                        int dist = Math.Abs(objCenterX - charCenterX);

                        // Pick the one closest to character (Minimum Distance)
                        if (dist < minDistanceX)
                        {
                            minDistanceX = dist;
                            bestX = objCenterX;
                            bestMatchVal = maxVal;
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    int dist = bestX - charCenterX;

                    OnStatusChanged?.Invoke($"Mục tiêu: {bestX} | KC: {dist}px (Gần nhất) | Score: {bestMatchVal:F2}");

                    if (Math.Abs(dist) <= AttackDistance)
                    {
                        _keySender.SendKey(AttackKey);
                    }
                    else
                    {
                        if (dist > 0) _keySender.SendKey(Keys.Right);
                        else _keySender.SendKey(Keys.Left);
                    }
                }
                else
                {
                    OnStatusChanged?.Invoke("Đang tìm quái gần nhất...");
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _templateImage?.Dispose();
        }
    }
}
