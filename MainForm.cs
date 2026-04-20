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
        #region Types
        /// <summary>One handled clipboard API message.</summary>
        record MsgSpec(string Name, Func<Message, int> Handler, string Description);
        #endregion

        #region Fields
        /// <summary>Cosmetics.</summary>
        readonly Color _drawColor = Color.LimeGreen;

        /// <summary></summary>
        readonly Keys _keyTrigger = Keys.Z;

        ///// <summary>Handle to the LL hook. Needed to unhook and call the next hook in the chain.</summary>
        //KBDLL readonly IntPtr _hhook = IntPtr.Zero;

        /// <summary>All clips in the collection.</summary>
        readonly LinkedList<ClipDisplay> _clips = new();

        ///// <summary>Key status.</summary>
        //KBDLL bool _letterPressed = false;

        ///// <summary>Key status.</summary>
        //KBDLL bool _winPressed = false;

        /// <summary>Manage resources.</summary>
        bool _disposed;

        const int MAX_CLIPS = 10;

        /// <summary>Hacks to work around win11 bug.</summary>
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

            Visible = false; // TODO doesn't work - like C:\Dev\Misc\NLab\TrayExForm

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
            lblLetter.Text = _keyTrigger.ToString();

            //KBDLL  Init LL keyboard hook.
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
            var res = AddClipboardFormatListener(Handle);

            if (!res) throw new InvalidOperationException("!!!!!!!!!!");

            base.OnLoad(e);
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
            //KBDLL CB.UnhookWindowsHookEx(_hhook);

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
            switch (m.Msg)
            {
                case W32.WM_CLIPBOARDUPDATE:
                    Tell($"WM_CLIPBOARDUPDATE");
                    CbDraw(m);
                    break;

                default:  // Ignore, pass along.
                    base.WndProc(ref m);
                    break;
            }
        }

        /// <summary>
        /// Process the clipboard draw message because contents have changed.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        int CbDraw(Message m)
        {
            int retries = 5;
            int ret = 99;

            Tell($"MESSAGE === CbDraw() HWnd:{m.HWnd} Msg:{m.Msg} WParam:{m.WParam} LParam:{m.LParam} ");

            while (ret != 0 && retries > 0)
            {
                try
                {
                    IDataObject? dobj = Clipboard.GetDataObject();

                    // Do something...
                    if (dobj is not null)
                    {
                        // Info about the originating window.
                        IntPtr hwnd = WM.ForegroundWindow;
                        var info = WM.GetAppWindowInfo(hwnd);
                        var process = Process.GetProcessById(info.Pid);
                        var procName = process.ProcessName;
                        var appName = "";
                        try { appName = Path.GetFileName(process.MainModule!.FileName); }
                        catch (Exception) { appName = "ERR BLOWED UP!!!!"; }

                        // Determine data type. This is only interested in text and images - all others are passed on to smarter clients.
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
                            // There appears to be a windows bug in KB5079473 that causes Prtint Screen to
                            // generate more than one message. This is a crude way to protect from that until MS fixes the issue.
                            // https://learn.microsoft.com/en-us/answers/questions/5593390/windows-11-25h2-snipping-tool-print-screen-saves-t
                            // https://learn.microsoft.com/en-us/answers/questions/5831588/there-are-two-copies-of-the-screenshot-in-the-clip

                            var bmp = Clipboard.GetImage() as Bitmap;
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
                            //else // suspect, wait

                            // Reset state.
                            _lastBmpTime = DateTime.Now;
                            _lastBmpSize = bmp.Size;
                        }
                        //else Something else, don't try to show it.

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
                            while (_clips.Count > MAX_CLIPS)
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

                            Tell($"New clip:{clip}");

                            Invalidate();
                        }

                        ret = 0;
                    }
                    else
                    {
                        Tell($"ERR CbDraw GetDataObject() is null");
                        retries--;
                    }
                }
                catch (ExternalException ex)
                {
                    // Retry: Data could not be retrieved from the Clipboard.
                    // This typically occurs when the Clipboard is being used by another process.
                    Tell($"ERR CbDraw WM_DRAWCLIPBOARD ExternalException:{ex}");
                    retries--;
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    Tell($"ERR CbDraw WM_DRAWCLIPBOARD Exception:{ex}");
                    retries = 0;
                }
            }

            return ret;
        }
        #endregion

        #region Paste Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="single"></param>
        /// <param name="button"></param>
        void ClipClick(ClipDisplay? clip, bool single, MouseButtons button)
        {
            if (single)
            {
                Clipboard.SetDataObject(clip.Data);
                DoPaste();
                // Push to head of class.
                _clips.Remove(clip);
                _clips.AddFirst(clip);

                Invalidate();
            }
            // else // double
        }

        /// <summary>
        /// Send paste to focus window.
        /// </summary>
        void DoPaste()
        {
            // TODO get the paste requester -- but the focus is me now! 
            //IntPtr hwnd = WM.ForegroundWindow;
            //var info = WM.GetAppWindowInfo(hwnd);
            //uint tid = WM.GetWindowThreadProcessId(hwnd, out uint lpdwProcessId);
            //var p = Process.GetProcessById((int)lpdwProcessId);
            //Tell($"FileName:{p.MainModule!.FileName} pid:{ lpdwProcessId} tid:{tid}");

            // This does work. Virtual keycodes from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            W32.InjectKey(W32.VK_CONTROL);
            W32.InjectKey('v');
            W32.InjectKey(W32.VK_CONTROL, up:true);
            W32.InjectKey('v', up: true);

            // Note that this doesn't work, which makes sense.
            //SendMessage(hwnd, 0x0302, IntPtr.Zero, IntPtr.Zero); // WM_PASTE
        }

        ///// <summary>
        ///// //KBDLL Low level hook function. Sniffs for magic key.
        ///// </summary>
        ///// <param name="code">If less than zero, pass the message to the CallNextHookEx function without further processing.</param>
        ///// <param name="wParam">One of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        ///// <param name="lParam">Pointer to a KBDLLHOOKSTRUCT structure.</param>
        ///// <returns></returns>
        //int KeyboardHookProc(int code, int wParam, ref CB.KBDLLHOOKSTRUCT lParam)
        //{
        //    if (code >= 0)
        //    {
        //        Keys key = (Keys)lParam.vkCode;
        //        // Tell($"KeyboardHookProc code:{code} wParam:{wParam} key:{key} scancode:{lParam.scanCode}");
        //        if (code >= 0)
        //        {
        //            // Update statuses.
        //            bool pressed = wParam == W32.WM_KEYDOWN || wParam == W32.WM_SYSKEYDOWN;
        //            //bool up = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;
        //            if (key == Keys.LWin || key == Keys.RWin)
        //            {
        //                _winPressed = pressed;
        //            }
        //            if (key == _keyTrigger)
        //            {
        //                _letterPressed = pressed;
        //            }
        //            bool match = _winPressed && _letterPressed;
        //            // Diagnostics.
        //            lblWin.BackColor = _winPressed ? _drawColor : Color.Transparent;
        //            lblLetter.BackColor = _letterPressed ? _drawColor : Color.Transparent;
        //            lblMatch.BackColor = match ? _drawColor : Color.Transparent;
        //            if (match)
        //            {
        //                // show UI;
        //                Visible = true;
        //            }
        //        }
        //    }
        //    return CB.CallNextHookEx(_hhook, code, wParam, ref lParam);
        //}
        #endregion

        #region Debug
        /// <summary>
        /// Do debug stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Debug_Click(object sender, EventArgs e)
        {
            foreach (var clip in _clips)
            {
                Tell(clip.ToString());
            }
        }

        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="msg"></param>
        void Tell(string msg)
        {
            string s = $"{DateTime.Now:hh\\:mm\\:ss\\.fff} {msg}";
            tvInfo.Append(s);
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