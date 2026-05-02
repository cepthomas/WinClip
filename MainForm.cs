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


// TODO - Frequently throws System.ExecutionEngineException. Previously indicated
// an unspecified fatal error in the runtime. MS says: The runtime no longer raises
// this exception so this type is obsolete.
// Other gleanings:
//   - Calling the Environment.FailFast method to terminate execution of an application running in the
//   Visual Studio debugger throws an ExecutionEngineException and automatically triggers the
//   fatalExecutionEngineError managed debugging assistant (MDA).
//   - Try disable hot reload/edit & continue.


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
        readonly List<ClipDisplay> _clips = new();

        /// <summary>Where to paste.</summary>
        IntPtr _pasteWin = IntPtr.Zero;

        /// <summary>Handle to the window event hook.</summary>
        readonly IntPtr _winEventHook = IntPtr.Zero;

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
            // Listen for window changes.
            _winEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                WindowEventCallback, 0, 0, WINEVENT_OUTOFCONTEXT); // | WINEVENT_SKIPOWNPROCESS);
            if (_winEventHook == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastWin32Error()); }

            // Listen for clipboard changes.
            var res = AddClipboardFormatListener(Handle);
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
            UnhookWinEvent(_winEventHook);

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

            // Send paste to the last window that was foreground, since WinClip is now fg.
            // Win11 does not allow direct setting of ForegroundWindow. This is now the way:
            // https://stackoverflow.com/questions/62966320/setforegroundwindow-not-setting-focus
            var fg = GetWindowInfo(_pasteWin);
            _logger.Debug($"Paste to win:{_pasteWin:X8} proc:{fg.ProcessId:X8}[{fg.ProcessName}]");
            Microsoft.VisualBasic.Interaction.AppActivate(fg.ProcessId);
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
            
            _clips.Add(clipd);
            
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
        }

        #region Drawing
        /// <summary>
        /// Draw the UI.
        /// </summary>
        /// <param name="e">The particular PaintEventArgs.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            int numGridColumns = 4;
            int pad = 5;

            // Assign ordered locations.
            int xinc = UserSettings.Settings.ClipSize.Width + pad;
            int yinc = UserSettings.Settings.ClipSize.Height + pad;

            for (int i = 0; i < _clips.Count; i++)
            {
                int row = i / numGridColumns;
                int col = i % numGridColumns;

                int xloc = xinc * col + pad;
                int yloc = yinc * row + btnDebug.Bottom + pad;

                _clips[i].Location = new Point(xloc, yloc);
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
            for (int i = 0; i < 20; i++)
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
            WindowInfo winfo = new(hwnd, appInfo.Pid, procName, title, appInfo.IsVisible);
            return winfo;
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
    record struct WindowInfo(IntPtr Hwnd, int ProcessId, string ProcessName, string Title, bool IsVisible)
    {
        public override readonly string ToString() { return $"hwnd:0X{Hwnd:X8} proc:{ProcessId:X8}[{ProcessName}] title:[{Title}] vis:{IsVisible}"; }
    };
    #endregion
}