using System;
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
    #region Types
    /// <summary>For internal management.</summary>
    public enum ClipType { Empty, PlainText, RichText, Bitmap, Other };
    #endregion

    /// <summary>
    /// - Handles all interactions at the Clipboard.XXX() API level.
    /// - Hooks keyboard to intercept magic paste key.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Current foreground window.</summary>
        readonly IntPtr _forewin = IntPtr.Zero;

        /// <summary>Handle to the LL hook.</summary>
        readonly IntPtr _hhook = IntPtr.Zero;

        /// <summary>All clips in the collection.</summary>
        readonly LinkedList<ClipDisplay> _clips = new();

        /// <summary>Manage resources.</summary>
        bool _disposed;

        // Hacks to work around win11 bug.
        // There appears to be a windows bug in KB5079473 that causes Prtint Screen to
        // generate more than one message. This is a crude way to protect from that until MS fixes the issue.
        // https://learn.microsoft.com/en-us/answers/questions/5593390/windows-11-25h2-snipping-tool-print-screen-saves-t
        // https://learn.microsoft.com/en-us/answers/questions/5831588/there-are-two-copies-of-the-screenshot-in-the-clip
        Size _lastBmpSize = new();
        DateTime _lastBmpTime = DateTime.Now;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            string appDir = MiscUtils.GetAppDataDir("WinClip", "Ephemera");
            // Load settings first before initializing.
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 100000);

            // Main form. TODO option to always show?
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            //WindowState = FormWindowState.Normal;
            Visible = false; // TODO doesn't work - like C:\Dev\Misc\NLab\TrayExForm - use Minimized?
            // Clean me up.
            var borderWidth = (Width - ClientSize.Width) / 2;
            //Width = x + borderWidth * 2;
            //Height = y + borderWidth * 2 + 55;
            //Width = _clips.First.Value.Width + borderWidth * 2 + 10;

            // Init controls.
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Matchers =
            [
                new("ERR", Color.Red),
                new("WRN", Color.Green),
            ];

            btnClear.Click += (_, __) => tvInfo.Clear();
            lblLetter.Text = "Z";// _keyTrigger.ToString();


            //////////////////////////////////////////////////////////////////////////////////////


            //  Init LL keyboard hook.
            using Process process = Process.GetCurrentProcess();
            using ProcessModule? module = process.MainModule;
            IntPtr hModule = W32.GetModuleHandle(module!.ModuleName!);
            _hhook = W32.SetWindowsHookEx(W32.WH_KEYBOARD_LL, KeyboardHookProc, hModule, 0);
            _logger.Debug($">>>10 APP hndModule:{hModule} _hhook:{_hhook}");

            //using Process process = Process.GetCurrentProcess();
            //using ProcessModule? module = process.MainModule;
            //// hMod: Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set
            ////   to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is
            ////   within the code associated with the current process.
            //// dwThreadId: Specifies the identifier of the thread with which the hook procedure is to be associated.If this parameter is
            ////   zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread.
            //IntPtr hModule = CB.GetModuleHandle(module!.ModuleName!);
            //_hhook = CB.SetWindowsHookEx(CB.HookType.WH_KEYBOARD_LL, KeyboardHookProc, hModule, 0);



            // Paste test.
            //_ticks = 5;
            //timer1.Tick += (_, __) => { if (_ticks-- > 0) { Clipboard.SetText($"XXXXX{_ticks}"); DoPaste(); } };
            //timer1.Enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected override void OnLoad(EventArgs e)
        {
            //GetProcess("MainForm()"); // needs hModule

            var res = AddClipboardFormatListener(Handle);

            W32.RegisterHotKey(Handle, (int)Keys.A, W32.MOD_ALT | W32.MOD_CTRL);
            W32.RegisterHotKey(Handle, (int)Keys.D9, W32.MOD_CTRL);

            if (!res) throw new InvalidOperationException("!!!!!!!!!!");

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
            W32.UnhookWindowsHookEx(_hhook);

            _disposed = true;

            base.Dispose(disposing);
        }
        #endregion

        #region Windows Message Processing - Cut/Copy/Paste
        /// <summary>
        /// Handle window messages.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // m.HWnd  A handle to the window whose window procedure receives the message
            // This member is NULL when the message is a thread message.

            // IntPtr handle = m.LParam;

            //base.WndProc(ref m);
            //return;

            if (m.Msg == W32.WM_CLIPBOARDUPDATE) // clipboard contents have changed
            {
                // Process the clipboard draw message because contents have changed.
                // was UpdateClipboard(m);
                int retries = 5;
                int ret = 99;

                _logger.Debug($"UpdateClipboard() HWnd:{m.HWnd} Msg:{m.Msg} WParam:{m.WParam} LParam:{m.LParam}");

                while (ret != 0 && retries > 0)
                {
                    try
                    {
                        IDataObject? dobj = Clipboard.GetDataObject();

                        if (dobj is not null)
                        {
                            // Info about the originating window.
                            //GetProcess("WndProc() FG", WM.ForegroundWindow);

                            IntPtr hwnd = WM.ForegroundWindow;
                            var info = WM.GetAppWindowInfo(hwnd);
                            var process = Process.GetProcessById(info.Pid);
                            var procName = process.ProcessName;
                            var appName = "";
                            try { appName = Path.GetFileName(process.MainModule!.FileName); }
                            catch (Exception) { appName = "ERR TODO BLOWED UP!!!!"; }

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
                                // Bug workaround.
                                TimeSpan ts = DateTime.Now - _lastBmpTime;
                                if (((DateTime.Now - _lastBmpTime).TotalMilliseconds > 250) || bmp.Size != _lastBmpSize)
                                {
                                    // Not the same so assume valid.
                                    clip = new() { Data = Clipboard.GetDataObject() };
                                    // Make a thumbnail scaled to available real estate.
                                    int tnHeight = clip.Height;
                                    int tnWidth = clip.Width * clip.Height / bmp.Height;
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
                                clip.OriginatingApp = appName ?? "Unknown";
                                clip.OriginatingTitle = info.Title.ToString();

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

                                // Assign locations.
                                int xloc = 5;
                                int yloc = 5;
                                int yinc = clip.Height + 5;

                                foreach (var cl in _clips)
                                {
                                    cl.Location = new Point(xloc, yloc);
                                    yloc += yinc;
                                }

                                _logger.Debug($"New clip:{clip}");

                                Invalidate();
                            }

                            ret = 0;
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

            else if (m.Msg == W32.WM_HOTKEY_MESSAGE_ID) // Decode key.
            {
                Keys key = Keys.None;
                int mod = (int)((long)m.LParam & 0xFFFF);
                int num = (int)(m.LParam >> 16);

                if (Enum.IsDefined(typeof(Keys), num))
                {
                    key = (Keys)Enum.ToObject(typeof(Keys), num);
                }
                // else do something?

                _logger.Debug($"WndProc Hotkey key:[{key}] ctrl:[{mod & W32.MOD_CTRL}] alt:[{mod & W32.MOD_ALT}]");
            }
            else  // 36  129 130
            {
                // Ignore, pass along.  ??
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// A clip tile was clicked.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="single"></param>
        /// <param name="button"></param>
        void ClipClick(ClipDisplay? clip, bool single, MouseButtons button)
        {
            if (single)
            {
                // Push into sys clipboard.
                Clipboard.SetDataObject(clip.Data);
                
                // was DoPaste();

                //// TODO get the paste requester -- but the focus is me now! 
                //IntPtr hwnd = WM.ForegroundWindow;
                //var info = WM.GetAppWindowInfo(hwnd);
                ////uint tid = WM.GetWindowThreadProcessId(hwnd, out uint lpdwProcessId);
                ////var p = Process.GetProcessById((int)lpdwProcessId);
                //var p = Process.GetProcessById(info.Pid);
                //Tell($">>>50 hwnd:{hwnd} FileName:{p.MainModule!.FileName} pid:{info.Pid}");// tid:{tid}");

                // This does work. Virtual keycodes from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
                W32.InjectKey(W32.VK_CONTROL);
                W32.InjectKey('v');
                W32.InjectKey(W32.VK_CONTROL, up:true);
                W32.InjectKey('v', up: true);

                // Note that this doesn't work, which makes sense.
                //SendMessage(hwnd, W32.WM_PASTE, IntPtr.Zero, IntPtr.Zero);

                // Push to head of our fifo.
                _clips.Remove(clip);
                _clips.AddFirst(clip);

                Invalidate();
            }
            // else double??
        }

        /// <summary>
        /// Low level hook function. Sniffs for magic key.
        /// </summary>
        /// <param name="code">Virtual-key code in the range 1 to 254. If less than zero, pass the message to the CallNextHookEx function without further processing.</param>
        /// <param name="wParam">One of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">Pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns></returns>
        int KeyboardHookProc(int code, int wParam, ref W32.KBDLLHOOKSTRUCT lParam)
        {
            bool handled = false;

            if (code >= 0 && code < 255)
            {
                //GetProcess("KeyboardHookProc() FG", WM.ForegroundWindow);
                Keys key = (Keys)lParam.vkCode;
                _logger.Debug($"KeyboardHookProc() code:{code} wParam:{wParam} key:{key} scanCode:{lParam.scanCode}");

                bool keyDown = wParam == W32.WM_KEYDOWN || wParam == W32.WM_SYSKEYDOWN;
                bool keyUp = wParam == W32.WM_KEYUP || wParam == W32.WM_SYSKEYUP;

                /// <summary>Key status.</summary>
                bool letterPressed = (key.ToString() == lblLetter.Text) && keyDown;

                /// <summary>Key status.</summary>
                bool winPressed = (key == Keys.LWin || key == Keys.RWin) && keyDown;

                bool match = winPressed && letterPressed;

                // Diagnostics.
                lblWin.BackColor = winPressed ? Color.LimeGreen : Color.Transparent;
                lblLetter.BackColor = letterPressed ? Color.LimeGreen : Color.Transparent;
                lblMatch.BackColor = match ? Color.LimeGreen : Color.Transparent;

                //if (match)
                //{
                //    // show UI;
                //    Visible = true;
                //}
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
                return W32.CallNextHookEx(_hhook, code, wParam, ref lParam);
            }
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
            // foreach (var (name, cat) in changes)
            // {
            //     switch (name)
            //     {
            //         case "DrawColor":
            //         case "SelectedColor":
            //             restart = true;
            //             break;
            //     }
            // }
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
            //this.InvokeIfRequired(_ => Tell(e.Message));
            this.InvokeIfRequired(_ => tvInfo.Append(e.Message));
        }

        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="msg"></param>
        void TellX(string msg)
        {
            string s = $"{DateTime.Now:hh\\:mm\\:ss\\.fff} {msg}";
            tvInfo.Append(s);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd">If 0, my process</param>
        void GetProcess(string who, IntPtr hwnd = 0)
        {
            Process? process = null;
            if (hwnd > 0)
            {
                var info = WM.GetAppWindowInfo(hwnd);
                process = Process.GetProcessById(info.Pid);
            }
            else
            {
                process = Process.GetCurrentProcess();
            }

            if (process is not null)
            {
                using ProcessModule? module = process.MainModule;
                IntPtr hModule = W32.GetModuleHandle(module!.ModuleName!); //XXX  hModule

                var procName = process.ProcessName;
                var modName = module.ModuleName;
                var appName = ""; //XXX
                try { appName = Path.GetFileName(process.MainModule!.FileName); }
                catch (Exception) { appName = "ERR TODO BLOWED UP!!!!"; }

                _logger.Debug($">>>70 who:[{who}] hwnd:[{hwnd}] module:[{module}] procName:[{procName}] modName:[{modName}] appName:[{appName}] ");
            }
        }

        /// <summary>Do debug stuff.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Debug_Click(object sender, EventArgs e)
        {
            foreach (var clip in _clips)
            {
                TellX(clip.ToString());
            }
        }
        #endregion

        #region Native
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        #endregion
    }
}