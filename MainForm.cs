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
        record struct WindowInfo(IntPtr hwnd, string ProcessName, string AppName, string Title);
        // record struct WindowInfo(IntPtr hwnd = IntPtr.Zero, string ProcessName = "???", string AppName = "???", string Title = "???");
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

        #region Hacks to work around win11 bug
        Size _lastBmpSize = new();
        DateTime _lastBmpTime = DateTime.Now;
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
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 100000);
            UpdateFromSettings();

            ///// Main form. TODO? init, show/hide/minimize....
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            //WindowState = FormWindowState.Normal;
            Visible = false; // doesn't work - like C:\Dev\Misc\NLab\TrayExForm - use Minimized?
            // Clean me up.
            //var borderWidth = (Width - ClientSize.Width) / 2;
            //Width = x + borderWidth * 2;
            //Height = y + borderWidth * 2 + 55;
            //Width = _clips.First.Value.Width + borderWidth * 2 + 10;

            ///// Init controls.
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Matchers =
            [
                new("ERR ", Color.Red),
                new("WRN ", Color.Green),
            ];
            btnClear.Click += (_, __) => tvInfo.Clear();

            lblLetter.Text = "Z";

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
            var key = hk.Key & ~0x20; // make it UC   // high-order word
            var mod = (hk.Ctrl ? W32.MOD_CTRL : 0) |  // low-order word
                (hk.Alt ? W32.MOD_ALT : 0) |
                (hk.Shift ? W32.MOD_SHIFT : 0) |
                (hk.Win ? W32.MOD_WIN : 0);
            W32.RegisterHotKey(Handle, key, mod);

            //var wi = GetWindowInfo(Handle);
            //_logger.Debug($"Myself init ProcessInfo::: {wi}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
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

        #region Message Processing
        /// <summary>
        /// Handle window messages.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // m.HWnd  A handle to the window whose window procedure receives the message - not sender
            // This member is NULL when the message is a thread message.

            if (m.Msg == W32.WM_CLIPBOARDUPDATE) // clipboard contents have changed
            {
                // It's possible to have contention for the clipboard so retry is ok.
                int retries = 5;

                //_logger.Debug($"UpdateClipboard() HWnd:0X{m.HWnd:X8} Msg:{m.Msg} WParam:{m.WParam} LParam:{m.LParam}");
                //_logger.Debug($"WndProc WM_CLIPBOARDUPDATE WindowInfo::: {GetWindowInfo(WM.ForegroundWindow)}");"

                while (retries > 0)
                {
                    try
                    {
                        IDataObject? dobj = Clipboard.GetDataObject();

                        if (dobj is not null)
                        {
                            // Determine data type. This application is only interested in text and images - all others are passed on to smarter clients.
                            ClipDisplay? clip = null;

                            if (Clipboard.ContainsText())
                            {
                                clip = new()
                                {
                                    ShortText = Clipboard.GetText().Left(80),
                                    Data = Clipboard.GetDataObject()
                                };
                            }
                            else if (Clipboard.ContainsImage())
                            {
                                var bmp = Clipboard.GetImage() as Bitmap;

                                // Hacks to work around win11 bug in KB5079473 that causes system Print Screen to generate
                                // more than one message. This is a crude way to protect from that until MS fixes the issue.
                                // https://learn.microsoft.com/en-us/answers/questions/5593390/windows-11-25h2-snipping-tool-print-screen-saves-t
                                // https://learn.microsoft.com/en-us/answers/questions/5831588/there-are-two-copies-of-the-screenshot-in-the-clip
                                TimeSpan ts = DateTime.Now - _lastBmpTime;
                                if (((DateTime.Now - _lastBmpTime).TotalMilliseconds > 250) || bmp.Size != _lastBmpSize)
                                {
                                    // Not the same so assume valid.
                                    clip = new() { Data = Clipboard.GetDataObject() };
                                    // Make a thumbnail scaled to available real estate.
                                    int tnHeight = ClipDisplay.Height;
                                    int tnWidth = ClipDisplay.Width * ClipDisplay.Height / bmp.Height;
                                    clip.Thumbnail = bmp.Resize(tnWidth, tnHeight);
                                }
                                //else suspect, wait

                                // Reset state.
                                _lastBmpTime = DateTime.Now;
                                _lastBmpSize = bmp.Size;
                            }
                            //else Something else, ignore.

                            if (clip != null)
                            {
                                clip.Data = Clipboard.GetDataObject();
//                                clip.OriginatingApp = appName ?? "Unknown";
//                                clip.OriginatingTitle = info.Title.ToString();

                                clip.MouseClick += (sender, e) => { ClipClick(sender as ClipDisplay, true, e.Button); };
                                clip.MouseDoubleClick += (sender, e) => { ClipClick(sender as ClipDisplay, false, e.Button); };

                                _clips.AddFirst(clip);
                                Controls.Add(clip);

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

                m.Result = -1; // means handled?
            }
            else if (m.Msg == W32.WM_HOTKEY_MESSAGE_ID)
            {
                // Could decode if we needed to handle more than one.

                // TODO Show UI to let user pick a clip to paste.

                m.Result = -1; // means handled?
            }
            else
            {
                // Ignore, pass along.
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// A clip tile was clicked.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="single"></param>
        /// <param name="button"></param>
        void ClipClick(ClipDisplay? clip, bool single, MouseButtons button) // TODO test
        {
            if (single)
            {
                // Push into sys clipboard.
                Clipboard.SetDataObject(clip.Data);

                // Send to the last window that was foreground, since this is now fg. 
                W32.SendMessage(_previousWin, W32.WM_PASTE, IntPtr.Zero, IntPtr.Zero);

                // Push to head of our fifo.
                _clips.Remove(clip);
                _clips.AddFirst(clip);

                // Restore last window to fg.
                WM.ForegroundWindow = _previousWin;

                Invalidate();

                // This does work. Virtual keycodes from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
                // W32.InjectKey(W32.VK_CONTROL);
                // W32.InjectKey('v');
                // W32.InjectKey(W32.VK_CONTROL, up:true);
                // W32.InjectKey('v', up: true);
            }
            // else double??
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
            int yinc = ClipDisplay.Height + 5;

            foreach (var cl in _clips)
            {
                cl.Location = new Point(xloc, yloc);
                yloc += yinc;
            }

            base.OnPaint(e);
        }
        #endregion

        #region Windows Hooks
        /// <summary>
        /// Low level hook function.
        /// </summary>x
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

                //vkCode: Check if this member equals VK_CONTROL(
                //), VK_LCONTROL(
                //), or VK_RCONTROL(
                //).
                //flags: Indicates if the key is pressed(
                //) or released(
                //).

                //var  cc = W32.VK_CONTROL;
                //if (lParam.vkCode == 'Z' && (GetKeyState(VK_CONTROL) < 0))
                //{
                //    // Ctrl+Z pressed
                //    return 1; // Block it
                //}

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

        /// <summary>
        /// Keeps track of foreground window for paste ops.
        /// </summary>
        void WindowEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd != _currentWin)
            {
                _previousWin = _currentWin;
                _currentWin = hwnd;
            }

            txtCurrentWin.Text = GetWindowInfo(_currentWin).ToString();
            txtPreviousWin.Text = GetWindowInfo(_previousWin).ToString();

            // _logger.Debug($"Current Win::: {GetWindowInfo(_currentWin)}");
            // _logger.Debug($"Previous Win::: {GetWindowInfo(_previousWin)}");
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
            Process? process = null;
            WindowInfo? wi = null;

            var info = WM.GetAppWindowInfo(hwnd);
            process = hwnd == 0 ? Process.GetCurrentProcess() : Process.GetProcessById(info.Pid);

            if (process is not null)
            {
                // https://stackoverflow.com/questions/9501771/how-to-avoid-a-win32-exception-when-accessing-process-mainmodule-filename-in-c                // It's possible to have contention for the clipboard so retry is ok.
                int retries = 5;

                while (retries > 0)
                {
                    try
                    {
                        using ProcessModule? module = process.MainModule;
                        IntPtr hModule = W32.GetModuleHandle(module!.ModuleName!);
                        var title = info.Title;
                        var procName = process.ProcessName;
                        var modName = module.ModuleName;
                        var appName = "???";

                        appName = Path.GetFileName(process.MainModule!.FileName);
                        wi = new(hwnd, procName, appName, title);
                        retries = 0; // done
                    }
                    catch (Win32Exception ex)
                    {
                        retries--;

                        if (retries > 0)
                        {
                            Thread.Sleep(50);
                        }
                        else
                        {
                            // Hard fail.
                            throw new InvalidOperationException("process.MainModule.FileName failed");
                        }
                    }
                }
            }

            return wi;
        }

        /// <summary>Do debug stuff.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Debug_Click(object sender, EventArgs e)
        {
            foreach (var clip in _clips)
            {
                tvInfo.Append(clip.Format());
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
        #endregion
    }
}