using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AutoKeyPresser.NativeApi;

namespace AutoKeyPresser.Services
{
    public enum SendMethod
    {
        SendInput_Extended,
        KeybdEvent_WithDelay,
        PostMessage_Async,
        SendMessage_Sync,
        Combined_All
    }

    /// <summary>
    /// Service for sending keyboard inputs using various methods.
    /// </summary>
    public class KeySender
    {
        public SendMethod CurrentMethod { get; set; } = SendMethod.Combined_All;
        public int KeyHoldTimeMs { get; set; } = 50;
        public bool AttachThread { get; set; } = true;
        
        // Target window for background mode
        public IntPtr TargetWindow { get; set; } = IntPtr.Zero;
        public string TargetWindowTitle { get; private set; } = "";
        public bool UseBackgroundMode { get; set; } = false;

        // Để track xem có đang trong game không
        private DateTime _lastSwitchTime = DateTime.MinValue;
        private bool _isCurrentlyInGame = false;

        public void SetTargetWindow(IntPtr hwnd)
        {
            TargetWindow = hwnd;
            if (hwnd != IntPtr.Zero)
            {
                var sb = new StringBuilder(256);
                Win32.GetWindowText(hwnd, sb, 256);
                TargetWindowTitle = sb.ToString();
            }
            else
            {
                TargetWindowTitle = "";
            }
        }

        public bool FindAndSetTargetWindow(string titleContains)
        {
            IntPtr found = IntPtr.Zero;
            Win32.EnumWindows((hwnd, lParam) =>
            {
                var sb = new StringBuilder(256);
                Win32.GetWindowText(hwnd, sb, 256);
                string title = sb.ToString();
                
                if (!string.IsNullOrEmpty(title) && 
                    title.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    Win32.IsWindowVisible(hwnd))
                {
                    found = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
            {
                SetTargetWindow(found);
                return true;
            }
            return false;
        }

        public List<(IntPtr Handle, string Title)> GetAllWindows()
        {
            var windows = new List<(IntPtr, string)>();
            
            Win32.EnumWindows((hwnd, lParam) =>
            {
                if (Win32.IsWindowVisible(hwnd))
                {
                    var sb = new StringBuilder(256);
                    Win32.GetWindowText(hwnd, sb, 256);
                    string title = sb.ToString();
                    
                    if (!string.IsNullOrEmpty(title) && title.Length > 1)
                    {
                        windows.Add((hwnd, title));
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public void CaptureCurrentWindow()
        {
            IntPtr hwnd = Win32.GetForegroundWindow();
            SetTargetWindow(hwnd);
        }

        public void SendKey(Keys key)
        {
            IntPtr hwnd;
            
            // Background mode: gửi đến cửa sổ cụ thể
            if (UseBackgroundMode && TargetWindow != IntPtr.Zero)
            {
                hwnd = TargetWindow;
                SendKeyToBackgroundWindow(key, hwnd);
                return;
            }
            
            // Foreground mode: gửi như bình thường
            hwnd = Win32.GetForegroundWindow();
            
            if (hwnd == IntPtr.Zero)
                return;

            if (AttachThread)
            {
                uint targetThreadId = Win32.GetWindowThreadProcessId(hwnd, out _);
                uint currentThreadId = Win32.GetCurrentThreadId();
                if (targetThreadId != currentThreadId)
                {
                    Win32.AttachThreadInput(currentThreadId, targetThreadId, true);
                    try { SendKeyInternal(key, hwnd); }
                    finally { Win32.AttachThreadInput(currentThreadId, targetThreadId, false); }
                    return;
                }
            }
            
            SendKeyInternal(key, hwnd);
        }

        /// <summary>
        /// Gửi key đến cửa sổ background - tối ưu để giảm switch
        /// </summary>
        private void SendKeyToBackgroundWindow(Keys key, IntPtr targetHwnd)
        {
            IntPtr currentForeground = Win32.GetForegroundWindow();
            
            // Nếu game đang được focus, gửi trực tiếp
            if (currentForeground == targetHwnd)
            {
                SendKeyInternal(key, targetHwnd);
                return;
            }
            
            // Thử PostMessage trước (không cần switch)
            if (CurrentMethod == SendMethod.PostMessage_Async)
            {
                SendWithPostMessage(key, targetHwnd);
                return;
            }
            
            if (CurrentMethod == SendMethod.SendMessage_Sync)
            {
                SendWithSendMessage(key, targetHwnd);
                return;
            }
            
            // Với Combined_All hoặc các method khác - cần switch
            // Tối ưu: Chỉ switch 1 lần và giữ focus trong khoảng thời gian ngắn
            QuickSwitchAndSend(key, targetHwnd, currentForeground);
        }

        /// <summary>
        /// Switch nhanh - focus game, gửi key, trả về ngay lập tức
        /// </summary>
        private void QuickSwitchAndSend(Keys key, IntPtr targetHwnd, IntPtr originalHwnd)
        {
            uint currentThreadId = Win32.GetCurrentThreadId();
            uint targetThreadId = Win32.GetWindowThreadProcessId(targetHwnd, out _);
            uint foregroundThreadId = Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out _);
            
            // Attach threads để có quyền set foreground
            bool attached1 = Win32.AttachThreadInput(currentThreadId, foregroundThreadId, true);
            bool attached2 = Win32.AttachThreadInput(currentThreadId, targetThreadId, true);
            
            try
            {
                // Focus game nhanh (không show, không activate - chỉ focus)
                Win32.BringWindowToTop(targetHwnd);
                Win32.SetForegroundWindow(targetHwnd);
                
                // Gửi key ngay lập tức không delay
                SendKeyFast(key);
                
                // Trả focus về NGAY LẬP TỨC (không sleep)
                if (originalHwnd != IntPtr.Zero && originalHwnd != targetHwnd)
                {
                    Win32.SetForegroundWindow(originalHwnd);
                }
            }
            finally
            {
                if (attached1) Win32.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                if (attached2) Win32.AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }

        /// <summary>
        /// Gửi key nhanh với delay tối thiểu
        /// </summary>
        private void SendKeyFast(Keys key)
        {
            ushort vk = (ushort)key;
            ushort scan = (ushort)Win32.MapVirtualKey(vk, 0);
            bool isExtended = key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right;
            uint flags = Win32.KEYEVENTF_SCANCODE;
            if (isExtended) flags |= Win32.KEYEVENTF_EXTENDEDKEY;

            Win32.INPUT[] inputs = new Win32.INPUT[2];
            
            // Key down
            inputs[0] = new Win32.INPUT
            {
                type = Win32.INPUT_KEYBOARD,
                U = new Win32.InputUnion
                {
                    ki = new Win32.KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = scan,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            
            // Key up
            inputs[1] = new Win32.INPUT
            {
                type = Win32.INPUT_KEYBOARD,
                U = new Win32.InputUnion
                {
                    ki = new Win32.KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = scan,
                        dwFlags = flags | Win32.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Gửi cả 2 events cùng lúc
            Win32.SendInput(2, inputs, Marshal.SizeOf(typeof(Win32.INPUT)));
        }

        private void SendKeyInternal(Keys key, IntPtr hwnd)
        {
            switch (CurrentMethod)
            {
                case SendMethod.SendInput_Extended:
                    SendWithSendInput(key);
                    break;
                case SendMethod.KeybdEvent_WithDelay:
                    SendWithKeybdEvent(key);
                    break;
                case SendMethod.PostMessage_Async:
                    SendWithPostMessage(key, hwnd);
                    break;
                case SendMethod.SendMessage_Sync:
                    SendWithSendMessage(key, hwnd);
                    break;
                case SendMethod.Combined_All:
                    SendWithSendInput(key);
                    Thread.Sleep(10);
                    SendWithKeybdEvent(key);
                    break;
            }
        }

        private void SendWithSendInput(Keys key)
        {
            ushort vk = (ushort)key;
            ushort scan = (ushort)Win32.MapVirtualKey(vk, 0);
            bool isExtended = key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right;
            uint flags = Win32.KEYEVENTF_SCANCODE;
            if (isExtended) flags |= Win32.KEYEVENTF_EXTENDEDKEY;

            Win32.INPUT[] inputs = new Win32.INPUT[1];
            inputs[0] = new Win32.INPUT
            {
                type = Win32.INPUT_KEYBOARD,
                U = new Win32.InputUnion
                {
                    ki = new Win32.KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = scan,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            Win32.SendInput(1, inputs, Marshal.SizeOf(typeof(Win32.INPUT)));
            Thread.Sleep(KeyHoldTimeMs);
            inputs[0].U.ki.dwFlags = flags | Win32.KEYEVENTF_KEYUP;
            Win32.SendInput(1, inputs, Marshal.SizeOf(typeof(Win32.INPUT)));
        }

        private void SendWithKeybdEvent(Keys key)
        {
            byte vk = (byte)key;
            byte scan = (byte)Win32.MapVirtualKey(vk, 0);
            Win32.keybd_event(vk, scan, 0, UIntPtr.Zero);
            Thread.Sleep(KeyHoldTimeMs);
            Win32.keybd_event(vk, scan, 2, UIntPtr.Zero);
        }

        private void SendWithPostMessage(Keys key, IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            uint scan = Win32.MapVirtualKey((uint)key, 0);
            IntPtr lParamDown = (IntPtr)((scan << 16) | 1);
            IntPtr lParamUp = (IntPtr)((scan << 16) | 0xC0000001);
            Win32.PostMessage(hwnd, Win32.WM_KEYDOWN, (IntPtr)key, lParamDown);
            Thread.Sleep(KeyHoldTimeMs);
            Win32.PostMessage(hwnd, Win32.WM_KEYUP, (IntPtr)key, lParamUp);
        }

        private void SendWithSendMessage(Keys key, IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            uint scan = Win32.MapVirtualKey((uint)key, 0);
            IntPtr lParamDown = (IntPtr)((scan << 16) | 1);
            IntPtr lParamUp = (IntPtr)((scan << 16) | 0xC0000001);
            Win32.SendMessage(hwnd, Win32.WM_KEYDOWN, (IntPtr)key, lParamDown);
            Thread.Sleep(KeyHoldTimeMs);
            Win32.SendMessage(hwnd, Win32.WM_KEYUP, (IntPtr)key, lParamUp);
        }
    }
}
