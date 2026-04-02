using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;


namespace WinClip // copied from NLab
{
    public static class Win32
    {
        #region Definitions - win32 messages etc
        ///// Windows messages.
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_SYSKEYDOWN = 0x104; // when the user presses the F10 key (menu bar) or holds down the ALT key and then presses another key
        public const int WM_SYSKEYUP = 0x105; // when the user releases a key that was pressed while the ALT key was held down
        public const int WM_DRAWCLIPBOARD = 0x0308;
        public const int WM_CHANGECBCHAIN = 0x030D;
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const int WM_DESTROYCLIPBOARD = 0x0307;
        public const int WM_ASKCBFORMATNAME = 0x030C;
        public const int WM_CLEAR = 0x0303;
        public const int WM_COPY = 0x0301;
        public const int WM_CUT = 0x0300;
        public const int WM_PASTE = 0x0302;
        public const int WM_HOTKEY_MESSAGE_ID = 0x0312;
        public const int WM_GETTEXT = 0x000D;

        ///// Show Commands
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_NORMAL = SW_SHOWNORMAL;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = SW_SHOWMAXIMIZED;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;
        public const int SW_MAX = SW_FORCEMINIMIZE;

        ///// Message Box Flags
        public const uint MB_OK = 0x00000000;
        public const uint MB_OKCANCEL = 0x00000001;
        public const uint MB_ABORTRETRYIGNORE = 0x00000002;
        public const uint MB_YESNOCANCEL = 0x00000003;
        public const uint MB_YESNO = 0x00000004;
        public const uint MB_RETRYCANCEL = 0x00000005;
        public const uint MB_CANCELTRYCONTINUE = 0x00000006;
        public const uint MB_ICONHAND = 0x00000010;
        public const uint MB_ICONQUESTION = 0x00000020;
        public const uint MB_ICONEXCLAMATION = 0x00000030;
        public const uint MB_ICONASTERISK = 0x00000040;
        public const uint MB_USERICON = 0x00000080;
        public const uint MB_ICONWARNING = MB_ICONEXCLAMATION;
        public const uint MB_ICONERROR = MB_ICONHAND;
        public const uint MB_ICONINFORMATION = MB_ICONASTERISK;
        public const uint MB_ICONSTOP = MB_ICONHAND;

        ///// Virtual keys - from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        // Populate as needed.
        public const byte VK_CONTROL = 0x11;

        ///// Key Modifiers
        public const int MOD_ALT = 0x0001;
        public const int MOD_CTRL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        ///// Shell Events
        public const int HSHELL_WINDOWCREATED = 1;
        public const int HSHELL_WINDOWDESTROYED = 2;
        public const int HSHELL_ACTIVATESHELLWINDOW = 3; // not used
        public const int HSHELL_WINDOWACTIVATED = 4;
        public const int HSHELL_GETMINRECT = 5;
        public const int HSHELL_REDRAW = 6;
        public const int HSHELL_TASKMAN = 7;
        public const int HSHELL_LANGUAGE = 8;
        public const int HSHELL_ACCESSIBILITYSTATE = 11;
        public const int HSHELL_APPCOMMAND = 12;

        ///// Shell Execute Mask Flags
        public const uint SEE_MASK_DEFAULT = 0x00000000;
        public const uint SEE_MASK_CLASSNAME = 0x00000001;
        public const uint SEE_MASK_CLASSKEY = 0x00000003;
        public const uint SEE_MASK_IDLIST = 0x00000004;
        public const uint SEE_MASK_INVOKEIDLIST = 0x0000000c;   // Note SEE_MASK_INVOKEIDLIST(0xC) implies SEE_MASK_IDLIST(0x04)
        public const uint SEE_MASK_HOTKEY = 0x00000020;
        public const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
        public const uint SEE_MASK_CONNECTNETDRV = 0x00000080;
        public const uint SEE_MASK_NOASYNC = 0x00000100;
        public const uint SEE_MASK_FLAG_DDEWAIT = SEE_MASK_NOASYNC;
        public const uint SEE_MASK_DOENVSUBST = 0x00000200;
        public const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
        public const uint SEE_MASK_UNICODE = 0x00004000;
        public const uint SEE_MASK_NO_CONSOLE = 0x00008000;
        public const uint SEE_MASK_ASYNCOK = 0x00100000;
        public const uint SEE_MASK_HMONITOR = 0x00200000;
        public const uint SEE_MASK_NOZONECHECKS = 0x00800000;
        public const uint SEE_MASK_NOQUERYCLASSSTORE = 0x01000000;
        public const uint SEE_MASK_WAITFORINPUTIDLE = 0x02000000;
        public const uint SEE_MASK_FLAG_LOG_USAGE = 0x04000000;
        #endregion

        #region Fields
        static readonly List<int> _hotKeyIds = [];
        #endregion

        #region API - general
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
        /// Register shell hook.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static int RegisterShellHook(IntPtr handle)
        {
            int msg = RegisterWindowMessage("SHELLHOOK"); // test for 0?
            _ = RegisterShellHookWindow(handle);
            return msg;
        }

        /// <summary>
        /// Deregister shell hook.
        /// </summary>
        /// <param name="handle"></param>
        public static void DeregisterShellHook(IntPtr handle)
        {
            _ = DeregisterShellHookWindow(handle);
        }

        /// <summary>
        /// Rudimentary management of hotkeys. Only supports one (global) handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="key"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static int RegisterHotKey(IntPtr handle, int key, int mod)
        {
            int id = mod ^ key ^ (int)handle;
            _ = RegisterHotKey(handle, id, mod, key);
            _hotKeyIds.Add(id);
            return id;
        }

        /// <summary>
        /// Rudimentary management of hotkeys. Only supports one (global) handle.
        /// </summary>
        /// <param name="handle"></param>
        public static void UnregisterHotKeys(IntPtr handle)
        {
            _hotKeyIds.ForEach(id => UnregisterHotKey(handle, id));
        }

        /// <summary>
        /// Generic limited modal message box. Just enough for a typical console or hidden application.
        /// </summary>
        /// <param name="message">Body.</param>
        /// <param name="caption">Caption.</param>
        /// <param name="error">Use error icon otherwise info.</param>
        /// <param name="ask">Use OK/cancel buttons.</param>
        /// <returns>True if OK </returns>
        public static bool MessageBox(string message, string caption, bool error = false, bool ask = false)
        {
            uint flags = error ? MB_ICONERROR : MB_ICONINFORMATION;

            if (ask)
            {
                flags |= MB_OKCANCEL;
                int res = MessageBox(IntPtr.Zero, message, caption, flags);
                return res == 1; //IDOK
            }
            else
            {
                _ = MessageBox(IntPtr.Zero, message, caption, flags);
            }

            return true;
        }

        /// <summary>
        /// Thunk.
        /// </summary>
        public static void DisableDpiScaling()
        {
            SetProcessDPIAware();
        }

        /// <summary>
        /// Streamlined version of the real function.
        /// </summary>
        /// <param name="verb">Standard verb</param>
        /// <param name="path">Where</param>
        /// <param name="hide">Hide the new window.</param>
        /// <returns>Standard error code</returns>
        public static int ShellExecute(string verb, string path, bool hide = false)
        {
            var ss = new List<string> { "edit", "explore", "find", "open", "print", "properties", "runas" };
            if (!ss.Contains(verb)) { throw new ArgumentException($"Invalid verb:{verb}"); }

            // If ShellExecute() succeeds, it returns a value greater than 32,
            //   else it returns an error value that indicates the cause of the failure.
            int res = (int)ShellExecute(IntPtr.Zero, verb, path, IntPtr.Zero, IntPtr.Zero,
                hide ? SW_HIDE : SW_NORMAL);

            return res > 32 ? 0 : res;
        }
        #endregion

        #region API - Window Management

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

        /// <summary>
        /// Get or set the fg.
        /// </summary>
        public static IntPtr ForegroundWindow
        {
            get { return GetForegroundWindow(); }
            set { SetForegroundWindow(value); }
        }

        /// <summary>
        /// Get the shell window.
        /// </summary>
        public static IntPtr ShellWindow
        {
            get { return GetShellWindow(); }
        }

        /// <summary>
        /// Get all pertinent/visible windows for the application. Ignores non-visible or non-titled (internal).
        /// Note that new explorers may be in the same process or separate ones. Depends on explorer user options.
        /// </summary>
        /// <param name="appName">The application name to filter on.</param>
        /// <param name="includeAnonymous">Include those without titles or base "Program Manager".</param>
        /// <returns>List of window infos.</returns>
        public static List<AppWindowInfo> GetAppWindows(string appName, bool includeAnonymous = false)
        {
            List<AppWindowInfo> winfos = [];
            List<IntPtr> procids = [];

            // Get all processes.
            foreach (var p in Process.GetProcessesByName(appName))
            {
                procids.Add(p.Id);
            }

            // Enumerate all windows. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows
            List<IntPtr> visHandles = [];
            bool addWindowHandle(IntPtr hWnd, IntPtr param) // callback
            {
                if (IsWindowVisible(hWnd))
                {
                    visHandles.Add(hWnd);
                }
                return true;
            }
            IntPtr param = IntPtr.Zero;
            EnumWindows(addWindowHandle, param);

            foreach (var vh in visHandles)
            {
                var wi = GetAppWindowInfo(vh);
                var realWin = wi.Title != "" && wi.Title != "Program Manager";
                if (procids.Contains(wi.Pid) && (includeAnonymous || realWin))
                {
                    winfos.Add(wi);
                }
            }

            return winfos;
        }

        /// <summary>
        /// Get main window(s) for the application. Could be multiple if more than one process.
        /// </summary>
        /// <param name="appName">The application name to filter on.</param>
        /// <returns>List of window handles.</returns>
        public static List<IntPtr> GetAppMainWindows(string appName)
        {
            List<IntPtr> handles = [];

            // Get all processes. There is one entry per separate process.
            // Get each main window.
            foreach (var p in Process.GetProcessesByName(appName))
            {
                handles.Add(p.MainWindowHandle);
            }

            return handles;
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

        /// <summary>
        /// Show the window.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>        
        public static bool ShowWindow(IntPtr handle)
        {
            return ShowWindow(handle, SW_SHOWNORMAL);
        }

        /// <summary>
        /// Move a window.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="loc">Where to.</param>
        /// <returns></returns>
        public static bool MoveWindow(IntPtr handle, Point loc)
        {
            bool ok = GetWindowRect(handle, out Rect rect);
            if (ok)
            {
                ok = MoveWindow(handle, loc.X, loc.Y, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
            }
            return ok;
        }

        /// <summary>
        /// Resize a window.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="size">How big is it.</param>
        /// <returns></returns>
        public static bool ResizeWindow(IntPtr handle, Size size)
        {
            bool ok = GetWindowRect(handle, out Rect rect);
            if (ok)
            {
                ok = MoveWindow(handle, rect.Left, rect.Top, size.Width, size.Height, true);
            }
            return ok;
        }
        #endregion

        #region Native Methods

        #region Types
        /// <summary>For ShellExecuteEx().</summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa
        /// ? Be careful with the string structure fields: UnmanagedType.LPTStr will be marshalled as unicode string so only
        /// the first character will be recognized by the function. Use UnmanagedType.LPStr instead.
        [StructLayout(LayoutKind.Sequential)]
        struct ShellExecuteInfo
        {
            // The size of this structure, in bytes.
            public int cbSize;
            // A combination of one or more ShellExecuteMaskFlags.
            public uint fMask;
            // Optional handle to the owner window.
            public IntPtr hwnd;
            // Specific operation.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            // null-terminated string that specifies the name of the file or object on which ShellExecuteEx will perform the action specified by the lpVerb parameter.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            // Optional null-terminated string that contains the application parameters separated by spaces.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            // Optional null-terminated string that specifies the name of the working directory.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            // Flags that specify how an application is to be shown when it is opened. See ShowCommands.
            public int nShow;
            // The rest are ?????
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

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

        #endregion

        #region shell32.dll
        /// <summary>Performs an operation on a specified file.
        /// Args: https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa.
        /// </summary>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, string lpParameters, string lpDirectory, int nShow);

        /// <summary>Overload of above for nullable args.</summary>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, IntPtr lpParameters, IntPtr lpDirectory, int nShow);

        /// <summary>Finer control version of above.</summary>
        [DllImport("shell32.dll", SetLastError = true)]
        static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);
        #endregion

        #region user32.dll
        /// <summary>Rudimentary UI notification for use in a console application.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int MessageBox(IntPtr hWnd, string msg, string caption, uint type);

        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern int RegisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern int DeregisterShellHookWindow(IntPtr hWnd);

        // Keyboard hooks.
        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        static extern int SendMessageInternal(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        #endregion


        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        extern static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>Retrieves a handle to the Shell's desktop window.</summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo winfo);

        /// <summary>Retrieves the thread and process ids that created the window.</summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr extraData);
        delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        /// <summary>Copies the text of the specified window's title bar (if it has one) into a buffer.</summary>
        /// <param name="hwnd">handle to the window</param>
        /// <param name="lpString">StringBuilder to receive the result</param>
        /// <param name="cch">Max number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated</param>
        [DllImport("user32.dll", EntryPoint = "GetWindowTextA", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthA", SetLastError = true, ExactSpelling = true)]
        static extern int GetWindowTextLength(IntPtr hwnd);
        #endregion
    }





    //#endregion
    //}
}
