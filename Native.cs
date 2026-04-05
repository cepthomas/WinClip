using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;


namespace WinClip
{
    /// <summary>Cherry-picked from NLab ref.</summary>
    public static class Native
    {
        #region Definitions - win32 messages etc
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_SYSKEYDOWN = 0x104; // when the user presses the F10 key (menu bar) or holds down the ALT key and then presses another key
        public const int WM_SYSKEYUP = 0x105; // when the user releases a key that was pressed while the ALT key was held down
        public const int WM_HOTKEY_MESSAGE_ID = 0x0312;
        public const int WM_GETTEXT = 0x000D;

        public const int WM_CLEAR = 0x0303;
        public const int WM_COPY = 0x0301;
        public const int WM_CUT = 0x0300;
        public const int WM_PASTE = 0x0302;

        public const int WM_DRAWCLIPBOARD = 0x0308;
        public const int WM_CHANGECBCHAIN = 0x030D;
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const int WM_DESTROYCLIPBOARD = 0x0307;
        public const int WM_ASKCBFORMATNAME = 0x030C;

        public const byte VK_CONTROL = 0x11;
        #endregion

        #region API functions
        public static IntPtr ForegroundWindow
        {
            get { return GetForegroundWindow(); }
            set { SetForegroundWindow(value); }
        }

        /// <summary>
        /// Generic message sender.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public static int SendMessage(IntPtr handle, int msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessageInternal(handle, msg, wParam, lParam);
        }

        /// <summary> 
        /// Inject a keystroke.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="up"></param>
        public static void InjectKey(byte key, bool up = false)
        {
            // TODO Should use SendInput() instead.
            keybd_event(key, 0, up ? 2 : 0, 0); // KEYEVENTF_KEYUP = 0x0002
        }

        /// <summary> 
        /// Inject a character.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="up"></param>
        public static void InjectKey(char key, bool up = false)
        {
            InjectKey((byte)key, up);
        }

        /// <summary>
        /// Get everything you need to know about a window.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>The info object.</returns>
        public static AppWindowInfo GetAppWindowInfo(IntPtr handle)
        {
            IntPtr threadId = GetWindowThreadProcessId(handle, out IntPtr pid);
            //GetWindowRect(handle, out Rect rect);

            StringBuilder sb = new(1024);
            _ = GetWindowText(handle, sb, sb.Capacity);

            WindowInfo wininfo = new();
            GetWindowInfo(handle, ref wininfo);

            // Helper.
            static Rectangle Convert(Rect rect)
            {
                return new()
                {
                    X = rect.Left,
                    Y = rect.Top,
                    Width = rect.Right - rect.Left,
                    Height = rect.Bottom - rect.Top
                };
            }

            AppWindowInfo wi = new()
            {
                Handle = handle,
                ThreadId = threadId,
                Pid = pid.ToInt32(),
                Parent = GetParent(handle),
                Title = sb.ToString(),
                IsVisible = IsWindowVisible(handle),
                DisplayRectangle = Convert(wininfo.rcWindow),
                ClientRectangle = Convert(wininfo.rcClient)
            };

            return wi;
        }

        /// <summary>Useful info about a window.</summary>
        public class AppWindowInfo
        {
            /// <summary>Native window handle.</summary>
            public IntPtr Handle { get; init; }

            /// <summary>Owner process.</summary>
            public int Pid { get; init; }

            /// <summary>Running on this thread.</summary>
            public IntPtr ThreadId { get; init; }

            /// <summary>Who's your daddy?</summary>
            public IntPtr Parent { get; init; }

            /// <summary>The coordinates of the window.</summary>
            public Rectangle DisplayRectangle { get; init; }

            /// <summary>The coordinates of the client area.</summary>
            public Rectangle ClientRectangle { get; init; }

            /// <summary>Window Text.</summary>
            public string Title { get; init; } = "";

            /// <summary>This is not trustworthy as it is true for some unseen windows.</summary>
            public bool IsVisible { get; set; }

            /// <summary>For humans.</summary>
            public override string ToString()
            {
                var g = $"X:{DisplayRectangle.Left} Y:{DisplayRectangle.Top} W:{DisplayRectangle.Width} H:{DisplayRectangle.Height}";
                var s = $"Title[{Title}] Geometry[{g}] IsVisible[{IsVisible}] Handle[{Handle}] Pid[{Pid}]";
                return s;
            }
        }
        #endregion

        #region Native functions
        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>Contains information about a window.</summary>
        [StructLayout(LayoutKind.Sequential)]
        struct WindowInfo
        {
            // The size of the structure, in bytes.The caller must set this member to sizeof(WindowInfo).
            public uint cbSize;
            // The coordinates of the window.
            public Rect rcWindow;
            // The coordinates of the client area.
            public Rect rcClient;
            // The window styles.For a table of window styles, see Window Styles.
            public uint dwStyle;
            // The extended window styles. For a table of extended window styles, see Extended Window Styles.
            public uint dwExStyle;
            // The window status.If this member is WS_ACTIVECAPTION (0x0001), the window is active.Otherwise, this member is zero.
            public uint dwWindowStatus;
            // The width of the window border, in pixels.
            public uint cxWindowBorders;
            // The height of the window border, in pixels.
            public uint cyWindowBorders;
            // The window class atom (see RegisterClass).
            public ushort atomWindowType;
            // The Windows version of the application that created the window.
            public ushort wCreatorVersion;
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo winfo);

        /// <summary>Retrieves the thread and process ids that created the window.</summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

        /// <summary>Copies the text of the specified window's title bar (if it has one) into a buffer.</summary>
        /// <param name="hwnd">handle to the window</param>
        /// <param name="lpString">StringBuilder to receive the result</param>
        /// <param name="cch">Max number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated</param>
        [DllImport("user32.dll", EntryPoint = "GetWindowTextA", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);

        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        static extern int SendMessageInternal(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        #endregion
    }
}
