using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Reflection;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using W32 = Ephemera.Win32.Internals;
using WM = Ephemera.Win32.WindowManagement;


// TODO! System.ExecutionEngineException - previously indicated an unspecified fatal error in the runtime.
//  The runtime no longer raises this exception so this type is obsolete.
// Calling the Environment.FailFast method to terminate execution of an application running in the
//   Visual Studio debugger throws an ExecutionEngineException and automatically triggers the
//   fatalExecutionEngineError managed debugging assistant (MDA).
// The System.ExecutionEngineException occurs in the most projects of the solution
//   if we try edit&continue.
// I tried to disable all Hot Reload related stuff



namespace WinClip
{
    /// <summary>
    /// - Handles all interactions at the Clipboard.XXX() API level.
    /// - Hooks keyboard to intercept magic paste key.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>All clips in the collection.</summary>
        readonly LinkedList<ClipDisplay> _clips = new();

        /// <summary>Where to paste.</summary>
        IntPtr _pasteWin = IntPtr.Zero;

        /// <summary>Handle to the LL key hook.</summary>
        readonly IntPtr _hHook = IntPtr.Zero;

        /// <summary>Handle to the window event hook.</summary>
        readonly IntPtr _windowEventHook = IntPtr.Zero;

        /// <summary>Manage resources.</summary>
        bool _disposed;

        /// <summary>Dev.</summary>
        Dev _dev = new();

        #region Workarounds 
        // win11 bug
        Size _lastBmpSize = new();
        DateTime _lastBmpTime = DateTime.Now;
        // multiple identical messages
        int _lastClipboardSeq = -1;
        #endregion
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            components = new Container();
            InitializeComponent();

            ///// Load settings and init logging.
            string appDir = MiscUtils.GetAppDataDir("WinClip", "Ephemera");
            UserSettings.Settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 100000);
            UpdateFromSettings();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            ///// Main form init.
            Location = UserSettings.Settings.FormGeometry.Location;
            ClientSize = new(UserSettings.Settings.ClipSize.Width + 30, UserSettings.Settings.FormGeometry.Size.Height);
            WindowState = FormWindowState.Normal;
            //ShowInTaskbar = false;
            //Visible = false; // doesn't work

            ///// System hooks.
            // LL keyboard hook.
            using Process process = Process.GetCurrentProcess();
            IntPtr hModule = W32.GetModuleHandle(process.MainModule!.ModuleName!);
            _hHook = W32.SetWindowsHookEx(W32.WH_KEYBOARD_LL, KeyboardHookProc, hModule, 0);

            // Listen for window changes.
            _windowEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                WindowEventCallback, 0, 0, WINEVENT_OUTOFCONTEXT); // | WINEVENT_SKIPOWNPROCESS);
            if (_windowEventHook == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastWin32Error()); }

            // Listen for clipboard changes.
            var res = AddClipboardFormatListener(Handle);

            // Listen for hot keys.
            AddHotKey(UserSettings.Settings.HotKey);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();

            // Save user settings.
            UserSettings.Settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            UserSettings.Settings.Save();

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Boilerplate.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // OK to use any private object references. Dispose managed state (managed objects).
                components.Dispose();
            }

            // Release unmanaged resources, set large fields to null.
            RemoveClipboardFormatListener(Handle);
            W32.UnhookWindowsHookEx(_hHook);
            UnhookWinEvent(_windowEventHook);

            _disposed = true;

            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// Handle window messages.
        /// </summary>
        /// <param name="msg"></param>
        protected override void WndProc(ref Message msg)
        {
            // m.HWnd  A handle to the window whose window procedure receives the message - not sender!
            // This member is NULL when the message is a thread message.

            switch (msg.Msg)
            {
                case W32.WM_CLIPBOARDUPDATE: // clipboard contents have changed
                    //_logger.Debug($"WM_CLIPBOARDUPDATE suppress:{_suppressClipboardUpdate}");
                    DoClipboardUpdate(msg);
                    msg.Result = -1; // means handled?
                    break;

                case W32.WM_HOTKEY_MESSAGE_ID:
                    DoHotkey(msg);
                    break;

                default:
                    // Ignore, pass along.
                    base.WndProc(ref msg);
                    break;
            }
        }

        /// <summary>
        /// Handle external clip copy ops.
        /// </summary>
        /// <param name="msg"></param>
        void DoClipboardUpdate(Message msg)
        {
            // It's possible to have contention for the clipboard so retry is ok.
            int retries = 5;

            while (retries > 0)
            {
                try
                {
                    IDataObject? dobj = Clipboard.GetDataObject();
                    // var tst = DateTime.Now.ToString("HH':'mm':'ss.fff");

                    // Sometimes we get multiples of the same message. Ignore subsequent of the same seq num.
                    var seq = GetClipboardSequenceNumber();
                    if (seq == _lastClipboardSeq)
                    {
                        return;
                    }
                    _lastClipboardSeq = (int)seq;

                    if (dobj is not null)
                    {
                        // Determine data type - only interested in text and images.
                        ClipBase? clip = null;

                        var fmts = dobj.GetFormats();
                        _dev.Tell($"Copy op: {string.Join("|", fmts)}");
                        if (fmts.Contains(ImageClip.TYPE_NAME))
                        {
                            // Hacks to work around win11 bug in KB5079473 that causes system Print Screen to generate
                            // more than one message. This is a crude way to protect from that until MS fixes the issue.
                            // https://learn.microsoft.com/en-us/answers/questions/5593390/windows-11-25h2-snipping-tool-print-screen-saves-t
                            // https://learn.microsoft.com/en-us/answers/questions/5831588/there-are-two-copies-of-the-screenshot-in-the-clip
                            TimeSpan ts = DateTime.Now - _lastBmpTime;
                            // _logger.Debug($"ts:{ts}");
                            int cutoff = 250; // measured is max 60 msec

                            var img = dobj.GetData(ImageClip.TYPE_NAME);
                            var bmp = img as Bitmap;

                            if (((DateTime.Now - _lastBmpTime).TotalMilliseconds > cutoff) || bmp.Size != _lastBmpSize)
                            {
                                // Not the same so assume valid.
                                clip = new ImageClip(dobj);
                            }
                            //else a suspect, wait

                            // Reset state.
                            _lastBmpTime = DateTime.Now;
                            _lastBmpSize = bmp.Size;
                        }
                        else if (fmts.Contains(RtfTextClip.TYPE_NAME))
                        {
                            clip = new RtfTextClip(dobj);
                        }
                        else if (fmts.Contains(PlainTextClip.TYPE_NAME))
                        {
                            clip = new PlainTextClip(dobj);
                        }
                        // else ignore

                        if (clip != null)
                        {
                            // Show it.
                            AddClip(clip);
                            //_logger.Debug($"New clip:{clip}");
                            Invalidate();
                        }

                        retries = 0; // done
                    }
                    else
                    {
                        // _logger.Warn($"GetDataObject() is null");
                        retries--;
                        Thread.Sleep(50);
                    }
                }
                catch (ExternalException ex)
                {
                    // _logger.Warn($"WM_DRAWCLIPBOARD ExternalException:{ex}");
                    retries--;
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Other exception:{ex}");
                    retries = 0;
                }
            }
        }

        /// <summary>
        /// Handle hotkey messages.
        /// </summary>
        /// <param name="m"></param>
        void DoHotkey(Message m)
        {
            // Could decode if we needed to handle more than one.

            // Get current window.
            var fgi = GetWindowInfo(WM.ForegroundWindow);
            _logger.Debug($">>> WM_HOTKEY_MESSAGE_ID {fgi}");

            // Show UI to let user pick a clip to paste.
            WindowState = FormWindowState.Normal;

            m.Result = -1; // means handled?
        }

        /// <summary>
        /// A clip was clicked.
        /// </summary>
        /// <param name="clipd">Sender</param>
        /// <param name="single">Single or double click</param>
        /// <param name="button">L or R button</param>
        void ClipClick(ClipDisplay? clipd, bool single, MouseButtons button)
        {
            if (clipd is null)
            {
                return;
            }

            // Remove UI from list.
            Controls.Remove(clipd);
            _clips.Remove(clipd);

            // Push into sys clipboard which will move it to the top.
            switch (clipd.Clip)
            {
                case PlainTextClip pcltxt:
                    Clipboard.SetData(PlainTextClip.TYPE_NAME, pcltxt.Content);
                    break;

                case RtfTextClip pclrtf:
                    Clipboard.SetData(RtfTextClip.TYPE_NAME, pclrtf.Content);
                    break;

                case ImageClip climg:
                    Clipboard.SetData(ImageClip.TYPE_NAME, climg.Content);
                    break;

                default:
                    // Ignore the impossible.
                    break;
            }

            // Send paste to the last window that was foreground, since this app is now fg.

            // https://stackoverflow.com/questions/62966320/setforegroundwindow-not-setting-focus

            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.interaction.appactivate?view=net-10.0
            // AppActivate(int ProcessId);
            // AppActivate("Untitled - Notepad")
            // Namespace: Microsoft.VisualBasic

            WM.ForegroundWindow = _pasteWin;
            var fg = GetWindowInfo(WM.ForegroundWindow);
            _logger.Debug($"Paste to {WM.ForegroundWindow} [{fg.ProcessName}]");


            SendKeys.Send("^{V}");

            Invalidate();
        }

        /// <summary>
        /// Add a new clip for viewing.
        /// </summary>
        /// <param name="clip"></param>
        void AddClip(ClipBase clip)
        {
            ClipDisplay clipd = new(clip)
            {
                Width = UserSettings.Settings.ClipSize.Width,
                Height = UserSettings.Settings.ClipSize.Height,
            };

            clipd.MouseClick += (sender, e) => { ClipClick(sender as ClipDisplay, true, e.Button); };
            clipd.MouseDoubleClick += (sender, e) => { ClipClick(sender as ClipDisplay, false, e.Button); };
            _clips.AddFirst(clipd);
            Controls.Add(clipd);

            // Limit - remove tail(s).
            while (_clips.Count > UserSettings.Settings.MaxClips)
            {
                var clipx = _clips.Last();
                Controls.Remove(clipx);
                _clips.Remove(clipx);
            }
        }

        /// <summary>
        /// Keeps track of foreground window for paste ops.
        /// </summary>
        void WindowEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var winfo = GetWindowInfo(hwnd);

            if (winfo.IsVisible && winfo.ProcessName != "WinClip" && winfo.ProcessName != "explorer")
            {
                _pasteWin = hwnd;
                _dev.Tell($"Set _pasteWin: {_pasteWin} [{GetWindowInfo(_pasteWin).ProcessName}]");
            }

            //var winfo = GetWindowInfo(hwnd);
            //if (hwnd != _currentWin && winfo.IsVisible && winfo.ProcessName != "explorer")
            //{
            //    _previousWin = _currentWin;
            //    _currentWin = hwnd;
            //    _dev.Tell($"Current: {GetWindowInfo(_currentWin).ProcessName}");
            //    _dev.Tell($"Previous: {GetWindowInfo(_previousWin).ProcessName}");
            //}
        }

        #region Drawing
        /// <summary>
        /// Draw the UI.
        /// </summary>
        /// <param name="e">The particular PaintEventArgs.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Assign ordered locations.
            int xloc = 5;
            int yloc = btnDebug.Bottom + 5;
            int yinc = UserSettings.Settings.ClipSize.Height + 5;

            foreach (var cl in _clips)
            {
                cl.Location = new Point(xloc, yloc);
                yloc += yinc;
            }

            base.OnPaint(e);
        }
        #endregion

        #region Settings
        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(UserSettings.Settings, "User Settings", 450);

            // Detect changes of interest.
            if (changes.Any(ch => ch.name == "ClipSize" || ch.name == "DisplayFont"))
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            UpdateFromSettings();

            UserSettings.Settings.Save();
        }

        /// <summary>
        /// 
        /// </summary>
        void UpdateFromSettings()
        {
            LogManager.MinLevelFile = UserSettings.Settings.FileLogLevel;
            LogManager.MinLevelNotif = UserSettings.Settings.NotifLogLevel;
        }
        #endregion

        #region Misc
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            this.InvokeIfRequired(_ => _dev.Tell(e.Message));
        }

        /// <summary>Do debug stuff.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Debug_Click(object sender, EventArgs e)
        {
            _dev.Show();
            for (int i = 0; i < 3; i++)
            {
                var sdir = MiscUtils.GetSourcePath();
                var fn = Path.Combine(sdir, "Test", "ross.txt");
                AddClip(new PlainTextClip(new DataObject(File.ReadAllText(fn))));
                fn = Path.Combine(sdir, "Test", "ex.rtf");
                AddClip(new RtfTextClip(new DataObject(RtfTextClip.TYPE_NAME, (File.ReadAllText(fn)))));
                fn = Path.Combine(sdir, "Test", "ex.png");
                AddClip(new ImageClip(new DataObject(Bitmap.FromFile(fn))));
            }

            // foreach (var clipd in _clips)
            // {
            //     _dev.Tell(clipd.Clip.Format());
            // }

            //List<IntPtr> appwins = WM.GetTopWindows(false);
            //foreach (IntPtr appwin in appwins)
            //{
            //    var winfo = GetWindowInfo(appwin);
            //    tvInfo.Append(winfo.ToString());
            //}
        }

        /// <summary>
        /// Get pertinent bits of info for a window.
        /// </summary>
        /// <param name="hwnd">If 0, my process</param>
        WindowInfo GetWindowInfo(IntPtr hwnd = 0)
        {
            var appInfo = WM.GetAppWindowInfo(hwnd);
            Process? process = hwnd == 0 ? Process.GetCurrentProcess() : Process.GetProcessById(appInfo.Pid);
            var title = appInfo.Title;
            var procName = process.ProcessName;
            WindowInfo winfo = new(hwnd, procName, title, appInfo.IsVisible, appInfo.Parent);
            return winfo;
        }

        /// <summary>
        /// 
        /// </summary>
        void AddHotKey(HotKey hk)
        {
            // Listen for hot keys.
            var key = hk.Key[0] & ~0x20; // make it UC   // high-order word
            var mod = (hk.Ctrl ? W32.MOD_CTRL : 0) |  // low-order word
                (hk.Alt ? W32.MOD_ALT : 0) |
                (hk.Shift ? W32.MOD_SHIFT : 0) |
                (hk.Win ? W32.MOD_WIN : 0);
            W32.RegisterHotKey(Handle, key, mod);
        }

        /// <summary>
        /// Low level keyboard hook function. Useful?? put in NDev/or? if not.
        /// </summary>
        /// <param name="code">Virtual-key code in the range 1 to 254. If less than zero, pass the message to the CallNextHookEx function without further processing.</param>
        /// <param name="wParam">One of the following messages: WM_KEYDOWN WM_KEYUP WM_SYSKEYDOWN WM_SYSKEYUP.</param>
        /// <param name="lParam">Pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>Return value from call to next in chain or >0 for handled locally</returns>
        int KeyboardHookProc(int code, int wParam, ref W32.KBDLLHOOKSTRUCT lParam)
        {
            bool handled = false;

            if (code >= 0)
            {
                Keys key = (Keys)lParam.vkCode;

                bool keyDown = wParam == W32.WM_KEYDOWN || wParam == W32.WM_SYSKEYDOWN;
                bool keyUp = wParam == W32.WM_KEYUP || wParam == W32.WM_SYSKEYUP;
                bool letterPressed = key == Keys.R && keyDown;
                bool winKey = (key == Keys.LWin || key == Keys.RWin) && keyDown;
                bool ctrlKey = (key & Keys.Control) > 0 && keyDown;
                bool altKey = (key & Keys.Alt) > 0 && keyDown;
            }

            if (handled)
            {
                // If the hook procedure processed the message, it may return a nonzero value to prevent
                // the system from passing the message to the rest of the hook chain or the target window procedure.
                return 1;
            }
            else
            {
                // Pass along chain.
                return W32.CallNextHookEx(_hHook, code, wParam, ref lParam);
            }
        }

        #endregion

        #region Native
        private const int WINEVENT_INCONTEXT = 4;
        private const int WINEVENT_OUTOFCONTEXT = 0;
        private const int WINEVENT_SKIPOWNPROCESS = 2;
        private const int WINEVENT_SKIPOWNTHREAD = 1;
        private const int EVENT_SYSTEM_FOREGROUND = 3;

        private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, int idProcess, int idThread, int dwflags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();
        #endregion
    }

    #region Types
    [Serializable]
    public sealed class HotKey
    {
        public string Key { get; set; } = "?";
        public bool Ctrl { get; set; } = false;
        public bool Alt { get; set; } = false;
        public bool Shift { get; set; } = false;
        public bool Win { get; set; } = false;
    }

    record struct WindowInfo(IntPtr Hwnd, string ProcessName, string Title, bool IsVisible, IntPtr Parent)
    {
        public override readonly string ToString() { return $"hwnd:0X{Hwnd:X8} proc:{ProcessName} title:{Title} vis:{IsVisible} parent:0X{Parent:X8}"; }
    };
    #endregion
}