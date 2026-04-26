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
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using W32 = Ephemera.Win32.Internals;
using WM = Ephemera.Win32.WindowManagement;


namespace WinClip
{
    /// <summary>
    /// - Handles all interactions at the Clipboard.XXX() API level.
    /// - Hooks keyboard to intercept magic paste key.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Types
        record struct WindowInfo(IntPtr Hwnd, string ProcessName, string Title, bool IsVisible, IntPtr Parent)
        {
            public override readonly string ToString() { return $"hwnd:0X{Hwnd:X8} proc:{ProcessName} title:{Title} vis:{IsVisible} parent:0X{Parent:X8}"; }
        }
        #endregion

        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>All clips in the collection.</summary>
        readonly LinkedList<ClipDisplay> _clips = new();

        /// <summary>Current foreground window handle.</summary>
        IntPtr _currentWin = IntPtr.Zero;

        /// <summary>Previous foreground window handle.</summary>
        IntPtr _previousWin = IntPtr.Zero;

        /// <summary>Handle to the LL hook.</summary>
        readonly IntPtr _hHook = IntPtr.Zero;

        /// <summary>Handle to the window event hook.</summary>
        readonly IntPtr _windowEventHook = IntPtr.Zero;

        /// <summary>Manage resources.</summary>
        bool _disposed;


        int _clipHeight = 50;
        int _clipWidth = 200;
        IntPtr _fg;
        bool _suppressClipboardUpdate = false;
        //public static int Height { get; set; } = 50;
        //public static int Width { get; set; } = 200;
        //public enum SelectorStyle { Tile, Icon }


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
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 100000);
            UpdateFromSettings();

            ///// Main form init.
            //ShowInTaskbar = false;
            //ShowInTaskbar = true;
            //WindowState = FormWindowState.Minimized;
            //StartPosition = FormStartPosition.Manual;

            //Location = new(500, 100);
            //Location = _settings.FormGeometry.Location;
            //Size = _settings.FormGeometry.Size;
            //Visible = false; // doesn't work - like C:\Dev\Misc\NLab\TrayExForm - use Minimized?

            ///// Init controls.
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Matchers =
            [
                new("ERR ", Color.Red),
                new("WRN ", Color.Green),
            ];
            btnClear.Click += (_, __) => tvInfo.Clear();

            lblLetter.Text = _settings.HotKey.Key;

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
            //? if (!res) throw new InvalidOperationException("!!!!!!!!!!");

            // Listen for hot keys.
            var hk = _settings.HotKey;
            var key = hk.Key[0] & ~0x20; // make it UC   // high-order word
            var mod = (hk.Ctrl ? W32.MOD_CTRL : 0) |  // low-order word
                (hk.Alt ? W32.MOD_ALT : 0) |
                (hk.Shift ? W32.MOD_SHIFT : 0) |
                (hk.Win ? W32.MOD_WIN : 0);
            W32.RegisterHotKey(Handle, key, mod);

            //var wi = GetWindowInfo(Handle);
            //_logger.Debug($"Myself init ProcessInfo::: {wi}");

            // Debug stuff 
            for (int i = 0; i < 4; i++)
            {
                var s = $">>>{i}>>> Nice little clouds playing around in the sky. I'm sort of a softy, I couldn't shoot Bambi except with a camera. We artists are a different breed of people.";
                var clip = new PlainTextClip(new DataObject(s));

                ClipDisplay clipd = new(clip)
                {
                    Width = _clipWidth,
                    Height = _clipHeight,
                };

                clipd.MouseClick += (sender, e) => { ClipClick(sender as ClipDisplay, true, e.Button); };
                clipd.MouseDoubleClick += (sender, e) => { ClipClick(sender as ClipDisplay, false, e.Button); };

                _clips.AddLast(clipd);
                Controls.Add(clipd);
            }
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();

            // Save user settings.
            _settings.FormGeometry = new()
            {
               X = Location.X,
               Y = Location.Y,
               Width = Width,
               Height = Height
            };

            _settings.Save();

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


        #region Hacks to work around win11 bug
        Size _lastBmpSize = new();
        DateTime _lastBmpTime = DateTime.Now;
        #endregion


        //DateTime _lastUpdateTime = DateTime.Now;

        //string _timeFormat = "yyyy'-'MM'-'dd HH':'mm':'ss.fff";
        string _timeFormat = "HH':'mm':'ss.fff";




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

                    // Multiple? wait...
                    //Thread.Sleep(100);

                    //_logger.Debug($"WM_CLIPBOARDUPDATE suppress:{_suppressClipboardUpdate}");


                    //if (!_suppressClipboardUpdate) // one shot
                    {
                        DoClipboardUpdate(msg);
                    }
                    // reset
                    _suppressClipboardUpdate = false;

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



        int _lastSeq = -1;


        /// <summary>
        /// Handle external clip updates.
        /// </summary>
        /// <param name="msg"></param>
        void DoClipboardUpdate(Message msg)
        {
            // m.HWnd  A handle to the window whose window procedure receives the message - not sender!
            // This member is NULL when the message is a thread message.






            // It's possible to have contention for the clipboard so retry is ok.
            int retries = 5;


            while (retries > 0)
            {
                try
                {
                    IDataObject? dobj = Clipboard.GetDataObject();

                    var tst = DateTime.Now.ToString(_timeFormat);
                    var hash = dobj.GetHashCode();

                    //Sometimes get multiples of the same message. Ignore subsequent of the same id.
                    //The usual mitigation strategy is to avoid reacting to every update, and react to the LAST
                    //update after a reasonable "settle time" has elapsed with no further clipboard notifications.
                    //500ms will usually be more than adequate.
                    var seq = GetClipboardSequenceNumber();
                    if (seq == _lastSeq)
                    {
                        return;
                    }



                    _logger.Debug($"DoClipboardUpdate() ts:{tst} hash:{hash} seq:{seq}");

                    _lastSeq = (int)seq;

                    if (dobj is not null)
                    {
                        // Determine data type. This application is only interested in text and images.
                        ClipBase? clip = null;
                        //ClipDisplay? clip = null;

                        var fmts = dobj.GetFormats();
                        if (fmts.Contains(ImageClip.TYPE_NAME))
                        {
                            // Hacks to work around win11 bug in KB5079473 that causes system Print Screen to generate
                            // more than one message. This is a crude way to protect from that until MS fixes the issue.
                            // https://learn.microsoft.com/en-us/answers/questions/5593390/windows-11-25h2-snipping-tool-print-screen-saves-t
                            // https://learn.microsoft.com/en-us/answers/questions/5831588/there-are-two-copies-of-the-screenshot-in-the-clip
                            var img = Clipboard.GetImage();
                            var bmp = img as Bitmap;
                            TimeSpan ts = DateTime.Now - _lastBmpTime;
                            _logger.Debug($"ts:{ts}");
                            int cutoff = 250; // measured like 60 msec

                            if (((DateTime.Now - _lastBmpTime).TotalMilliseconds > cutoff) || bmp.Size != _lastBmpSize)
                            {
                                // Not the same so assume valid.
                                clip = new ImageClip(dobj, _clipWidth, _clipHeight);
                            }
                            //else suspect, wait

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
                        else
                        {
                            // TODO
                        }



                        //if (Clipboard.ContainsText())
                        //{
                        //    if (fmts.Contains("System.Drawing.Bitmap")) DataType = ClipType.Bitmap;
                        //    else if (fmts.Contains("Rich Text Format")) DataType = ClipType.RichText;
                        //    //var txt = Clipboard.GetText();
                        //    clip = new TextClip(dobj);
                        //    //clip = new()
                        //    //{
                        //    //    Data = new DataObject(txt),
                        //    //    //Width = _clipWidth,
                        //    //    //Height = _clipHeight,
                        //    //    ShortText = txt.Left(80),
                        //    //};
                        //}
                        //else if (Clipboard.ContainsImage())
                        //{
                        //    //var data = Clipboard.GetDataObject();
                        //    //var fmts = data.GetFormats();
                        //    var img = Clipboard.GetImage();
                        //    var bmp = img as Bitmap;

                        //    // Hacks to work around win11 bug in KB5079473 that causes system Print Screen to generate
                        //    // more than one message. This is a crude way to protect from that until MS fixes the issue.
                        //    // https://learn.microsoft.com/en-us/answers/questions/5593390/windows-11-25h2-snipping-tool-print-screen-saves-t
                        //    // https://learn.microsoft.com/en-us/answers/questions/5831588/there-are-two-copies-of-the-screenshot-in-the-clip
                        //    TimeSpan ts = DateTime.Now - _lastBmpTime;
                        //    _logger.Debug($"ts:{ts}");
                        //    int cutoff = 250; // measured like 60 msec

                        //    if (((DateTime.Now - _lastBmpTime).TotalMilliseconds > cutoff) || bmp.Size != _lastBmpSize)
                        //    {
                        //        // Not the same so assume valid.
                        //        clip = new ImageClip(dobj, _clipWidth, _clipHeight);

                        //        //// Make a thumbnail scaled to available real estate.
                        //        //int tnHeight = _clipHeight;
                        //        //int tnWidth = _clipWidth * _clipHeight / bmp.Height;
                        //        ////clip.Thumbnail = bmp.Resize(tnWidth, tnHeight);
                        //        //clip = new()
                        //        //{
                        //        //    Data = new DataObject(bmp),
                        //        //    //Width = _clipWidth,
                        //        //    //Height = _clipHeight,
                        //        //    Thumbnail = bmp.Resize(tnWidth, tnHeight)
                        //        //};
                        //    }
                        //    //else suspect, wait

                        //    // Reset state.
                        //    _lastBmpTime = DateTime.Now;
                        //    _lastBmpSize = bmp.Size;
                        //}
                        //// Something else, ignore

                        if (clip != null)
                        {
                            //clip.Data = Clipboard.GetDataObject();
                            //clip.OriginatingApp = appName ?? "Unknown";
                            //clip.OriginatingTitle = info.Title.ToString();

                            ClipDisplay clipd = new(clip);

                            clipd.MouseClick += (sender, e) => { ClipClick(sender as ClipDisplay, true, e.Button); };
                            clipd.MouseDoubleClick += (sender, e) => { ClipClick(sender as ClipDisplay, false, e.Button); };

                            _clips.AddFirst(clipd);
                            Controls.Add(clipd);

                            // Limit - remove tail(s).
                            while (_clips.Count > _settings.MaxClips)
                            {
                                var clipx = _clips.Last();
                                Controls.Remove(clipx);
                                _clips.Remove(clipx);
                            }

                            _logger.Debug($"New clip:{clip}");

                            Invalidate();
                        }

                        retries = 0; // done
                    }
                    else
                    {
                        _logger.Warn($"WndProc GetDataObject() is null");
                        retries--;
                    }
                }
                catch (ExternalException ex)
                {
                    // Retry: Data could not be retrieved from the Clipboard.
                    // This typically occurs when the Clipboard is being used by another process.
                    _logger.Warn($"WndProc WM_DRAWCLIPBOARD ExternalException:{ex}");
                    retries--;
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"WndProc WM_DRAWCLIPBOARD Exception:{ex}");
                    retries = 0;
                }
            }
        }

        /// <summary>
        /// A clip was clicked.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="single"></param>
        /// <param name="button"></param>
        void ClipClick(ClipDisplay clipd, bool single, MouseButtons button) // TODO test
        {
            if (single)
            {
                switch (clipd.Clip)
                {
                    case PlainTextClip pcltxt:
                        //var pt = pcltxt.Data;
                        // remove from list.
                        Controls.Remove(clipd);
                        _clips.Remove(clipd);
                        // Push into sys clipboard which will move it to the top.
                        _suppressClipboardUpdate = true;
                        // _logger.Debug($"Clipboard.SetData(System.String) in");
                        Clipboard.SetData(PlainTextClip.TYPE_NAME, pcltxt.Data); //TODO make a "safe" SetData()?
                        break;

                    case RtfTextClip pcltxt:
                        //var pt = pcltxt.Data;
                        // remove from list.
                        Controls.Remove(clipd);
                        _clips.Remove(clipd);
                        // Push into sys clipboard which will move it to the top.
                        _suppressClipboardUpdate = true;
                        // _logger.Debug($"Clipboard.SetData(System.String) in");
                        Clipboard.SetData(RtfTextClip.TYPE_NAME, pcltxt.Data); //TODO make a "safe" SetData()?
                        break;

                    case ImageClip climg:
                        // remove from list.
                        Controls.Remove(clipd);
                        _clips.Remove(clipd);
                        // Push into sys clipboard which will move it to the top.
                        _suppressClipboardUpdate = true;
                        Clipboard.SetData(ImageClip.TYPE_NAME, climg.Data);
                        break;

                    default:
                        _logger.Error("TODO error");
                        break;
                }

                //        switch (clipd.DataType)
                //        {
                //            case ClipDisplay.ClipType.PlainText:
                //                var pt = clip.Data.GetData("Text");
                //                // remove from list.
                //                Controls.Remove(clipd);
                //                _clips.Remove(clipd);
                //                // Push into sys clipboard which will move it to the top.
                //                _suppressClipboardUpdate = true;
                // //               _logger.Debug($"Clipboard.SetData(System.String) in");
                //                Clipboard.SetData("System.String", pt); //TODO make a "safe" SetData()?
                ////                _logger.Debug($"Clipboard.SetData(System.String) out");
                //                break;

                //            case ClipDisplay.ClipType.RichText:
                //                var rt = clip.Data.GetData("Text");
                //                // remove from list.
                //                Controls.Remove(clipd);
                //                _clips.Remove(clipd);
                //                // Push into sys clipboard which will move it to the top.
                //                _suppressClipboardUpdate = true;
                //                Clipboard.SetData("Rich Text Format", rt);
                //                break;

                //            case ClipDisplay.ClipType.Bitmap:
                //                var bmp = clip.Data.GetData("System.Drawing.Bitmap");
                //                // remove from list.
                //                Controls.Remove(clipd);
                //                _clips.Remove(clipd);
                //                // Push into sys clipboard which will move it to the top.
                //                _suppressClipboardUpdate = true;
                //                Clipboard.SetData("System.Drawing.Bitmap", bmp);
                //                break;

                //            default:
                //                _logger.Error("TODO error");
                //                break;
                //        }

                // Send paste to the last window that was foreground, since this is now fg. 
                WM.ForegroundWindow = _previousWin;
                var fg = GetWindowInfo(WM.ForegroundWindow);
               _logger.Debug($"Paste to [{fg.Value.ProcessName}]");
                SendKeys.Send("^{V}");

                Invalidate();
            }
            // else double??
        }

        /// <summary>
        /// Keeps track of foreground window for paste ops.
        /// </summary>
        void WindowEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd != _currentWin)
            {
                var winfo = GetWindowInfo(hwnd).Value;

                if (winfo.IsVisible && winfo.ProcessName != "explorer")
                {
                    _previousWin = _currentWin;
                    _currentWin = hwnd;

                    txtCurrentWin.Text = $"{GetWindowInfo(_currentWin).Value.Title}";
                    txtPreviousWin.Text = $"{GetWindowInfo(_previousWin).Value.Title}";
                }
            }
        }




        #region TODO useful??///////////////////////////////////////////////////////

        /// <summary>
        /// Handle hotkey messages.
        /// </summary>
        /// <param name="m"></param>
        void DoHotkey(Message m) // TODO is this useful now? put in NDev if not.
        {
            // m.HWnd  A handle to the window whose window procedure receives the message - not sender!
            // This member is NULL when the message is a thread message.

            // Could decode if we needed to handle more than one.

            //// Get current window.
            //_fg = WM.ForegroundWindow;
            //var fgi = GetWindowInfo(_fg);
            //_logger.Debug($">>> WM_HOTKEY_MESSAGE_ID {fgi}");

            //// Show UI to let user pick a clip to paste.
            //WindowState = FormWindowState.Normal;

            m.Result = -1; // means handled?
        }

        /// <summary>
        /// Low level keyboard hook function.
        /// </summary>
        /// <param name="code">Virtual-key code in the range 1 to 254. If less than zero, pass the message to the CallNextHookEx function without further processing.</param>
        /// <param name="wParam">One of the following messages: WM_KEYDOWN WM_KEYUP WM_SYSKEYDOWN WM_SYSKEYUP.</param>
        /// <param name="lParam">Pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>Return value from call to next in chain or >0 for handled locally</returns>
        int KeyboardHookProc(int code, int wParam, ref W32.KBDLLHOOKSTRUCT lParam) // TODO is this useful now? put in NDev if not.
        {
            bool handled = false;

            if (code >= 0)// && code < 255)
            {
                Keys key = (Keys)lParam.vkCode;

                //lParam.scanCode

                bool keyDown = wParam == W32.WM_KEYDOWN || wParam == W32.WM_SYSKEYDOWN;
                bool keyUp = wParam == W32.WM_KEYUP || wParam == W32.WM_SYSKEYUP;
                bool letterPressed = (key.ToString() == lblLetter.Text) && keyDown;
                bool winKey = (key == Keys.LWin || key == Keys.RWin) && keyDown;
                bool ctrlKey = (key & Keys.Control) > 0 && keyDown;
                bool altKey = (key & Keys.Alt) > 0 && keyDown;

                // Diagnostics.
                lblCtrl.BackColor = ctrlKey ? Color.LimeGreen : SystemColors.Control;
                lblAlt.BackColor = altKey ? Color.LimeGreen : SystemColors.Control;
                lblWin.BackColor = winKey ? Color.LimeGreen : SystemColors.Control;
                lblLetter.BackColor = letterPressed ? Color.LimeGreen : SystemColors.Control;
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




        #region Drawing
        /// <summary>
        /// Draw the UI.
        /// </summary>
        /// <param name="e">The particular PaintEventArgs.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Assign ordered locations.
            int xloc = 5;
            int yloc = 5;
            int yinc = _clipHeight + 5;

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
            var changes = SettingsEditor.Edit(_settings, "User Settings", 450);

            // Detect changes of interest.
            bool restart = false;
            foreach (var (name, cat) in changes) { }
            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            UpdateFromSettings();

            _settings.Save();
        }

        /// <summary>
        /// 
        /// </summary>
        void UpdateFromSettings()
        {
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
        }
        #endregion

        #region Private
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            this.InvokeIfRequired(_ => tvInfo.Append(e.Message));
        }

        /// <summary>
        /// Get pertinent bits of info for a window. TODO put in common?
        /// </summary>
        /// <param name="hwnd">If 0, my process</param>
        WindowInfo? GetWindowInfo(IntPtr hwnd = 0)
        {
            var appInfo = WM.GetAppWindowInfo(hwnd);
            Process? process = hwnd == 0 ? Process.GetCurrentProcess() : Process.GetProcessById(appInfo.Pid);
            var title = appInfo.Title;
            var procName = process.ProcessName;
            WindowInfo? winfo = new(hwnd, procName, title, appInfo.IsVisible, appInfo.Parent);
            return winfo;
        }

        /// <summary>Do debug stuff.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Debug_Click(object sender, EventArgs e)
        {
            //foreach (var clip in _clips)
            //{
            //    tvInfo.Append(clip.Format());
            //}

            List<IntPtr> appwins = WM.GetTopWindows(false);
            foreach (IntPtr appwin in appwins)
            {
                var winfo = GetWindowInfo(appwin);
                tvInfo.Append(winfo.ToString());
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
}