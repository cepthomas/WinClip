using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using W32 = Ephemera.Win32.Internals;
using WM = Ephemera.Win32.WindowManagement;
using CB = WinClip.Native;
using System.Text;

namespace WinClip
{
    #region Types
    /// <summary>For internal management.</summary>
    public enum ClipType { Empty, PlainText, RichText, Image, Other };
    #endregion

    /// <summary>
    /// - Handles all interactions at the Clipboard.XXX() API level.
    /// - Hooks keyboard to intercept magic paste key.
    /// </summary>
    public partial class WinClip : Form
    {
        #region Types
        /// <summary>One handled clipboard API message.</summary>
        record MsgSpec(string Name, Func<Message, int> Handler, string Description);

        // /// <summary>One entry in the collection. // TODO persist clip data.</summary>
        // class Clip 
        // {
        //     public object? Data { get; set; } = null;
        //     public ClipType Ctype { get; set; } = ClipType.Empty;
        //     public string Text { get; set; } = "";
        //     public Bitmap? Bitmap { get; set; } = null;
        //     public string OrigApp { get; set; } = "Unknown";
        //     public string OrigTitle { get; set; } = "Unknown";
        //     public override string ToString() => $"Ctype:{Ctype}";
        // }
        #endregion

        #region Fields
        /// <summary>Cosmetics.</summary>
        readonly Color _drawColor = Color.LimeGreen;

        ///// <summary></summary>
        //readonly bool _fitImage = true;

        /// <summary></summary>
        readonly Keys _keyTrigger = Keys.Z;

        /// <summary></summary>
        readonly bool _debug = true;

        /// <summary>Next in line for clipboard  notification.</summary>
        IntPtr _nextCb = IntPtr.Zero;

        /// <summary>Handle to the LL hook. Needed to unhook and call the next hook in the chain.</summary>
        readonly IntPtr _hhook = IntPtr.Zero;

        /// <summary>All handled clipboard API messages.</summary>
        readonly Dictionary<int, MsgSpec> _clipboardMessages = [];

        /// <summary>All clips in the collection.</summary>
        readonly LinkedList<ClipDisplay_1> _clips = new();
        // readonly LinkedList<Clip> _clips = new();

        // /// <summary>All clip displays.</summary>
        // readonly List<ClipDisplay> _displays = [];

        /// <summary>Key status.</summary>
        bool _letterPressed = false;

        /// <summary>Key status.</summary>
        bool _winPressed = false;

        /// <summary>Manage resources.</summary>
        bool _disposed;

        const int MAX_CLIPS = 10;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public WinClip()
        {
            InitializeComponent();

            string appDir = MiscUtils.GetAppDataDir("WinClip", "Ephemera");

            Visible = _debug; //TODO doesn't work - like C:\Dev\Misc\NLab\TrayExForm

            //// Init clip displays.
            //int x = 5;
            //int y = 5;
            //for (int i = 0; i < MAX_CLIPS; i++)
            //{
            //    ClipDisplay_1 cd = new() { Location = new Point(x, y), Id = i };
            //    _clips.AddLast(cd);
            //    Controls.Add(cd);
            //    //x = cd.Right + 5;
            //    y = cd.Bottom + 5;
            //    //_displays.Add(cd);
            //    //Controls.Add(cd);
            //    //cd.ClipRequest += Cd_ClipRequest;
            //    ////x = cd.Right + 5;
            //    //y = cd.Bottom + 5;
            //}

//            UpdateClipDisplays();

            // Clean me up.
            var borderWidth = (Width - ClientSize.Width) / 2;
            //Width = x + borderWidth * 2;
            //Height = y + borderWidth * 2 + 55;

            if (!_debug)
            {
                //Height = h + 55;
                Width = _clips.First.Value.Width + borderWidth * 2 + 10;
            }

            // Init controls.
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Matchers =
            [
                new("ERR", Color.Red),
                new("WRN", Color.Green),
            ];

            if (_debug)
            {
               // rtbText.LoadFile(@"..\..\\ex.rtf");
            }

            btnClear.Click += (_, __) => tvInfo.Clear();
            lblLetter.Text = _keyTrigger.ToString();

            _nextCb = CB.SetClipboardViewer(Handle);

            // HL messages of interest.
            _clipboardMessages = new()
            {
                { W32.WM_DRAWCLIPBOARD, new("WM_DRAWCLIPBOARD", CbDraw, "Sent to the first window in the clipboard viewer chain when the content of the clipboard changes aka copy/cut.") },
                { W32.WM_CHANGECBCHAIN, new("WM_CHANGECBCHAIN", CbChange, "Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.") },
                { W32.WM_CLIPBOARDUPDATE, new("WM_CLIPBOARDUPDATE", CbDefault, "Sent when the contents of the clipboard have changed.") },
                { W32.WM_DESTROYCLIPBOARD, new("WM_DESTROYCLIPBOARD", CbDefault, "Sent to the clipboard owner when a call to the EmptyClipboard function empties the clipboard.") },
                { W32.WM_ASKCBFORMATNAME, new("WM_ASKCBFORMATNAME", CbDefault, "Sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.") },
                { W32.WM_CLEAR, new("WM_CLEAR", CbDefault, "Clear") },
                { W32.WM_COPY, new("WM_COPY", CbDefault, "Copy") },
                { W32.WM_CUT, new("WM_CUT", CbDefault, "Cut") },
                { W32.WM_PASTE, new("WM_PASTE", CbDefault, "Paste") }
            };

            // Init LL keyboard hook.
            using Process process = Process.GetCurrentProcess();
            using ProcessModule? module = process.MainModule;
            // hMod: Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set
            //   to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is
            //   within the code associated with the current process.
            // dwThreadId: Specifies the identifier of the thread with which the hook procedure is to be associated.If this parameter is
            //   zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread.
            IntPtr hModule = CB.GetModuleHandle(module!.ModuleName!);
            _hhook = CB.SetWindowsHookEx(CB.HookType.WH_KEYBOARD_LL, KeyboardHookProc, hModule, 0);

            // Paste test.
            //_ticks = 5;
            //timer1.Tick += (_, __) => { if (_ticks-- > 0) { Clipboard.SetText($"XXXXX{_ticks}"); DoPaste(); } };
            //timer1.Enabled = true;
        }

        /// <summary>
        /// Boilerplate.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // OK to use any private object references.
                // Dispose managed state (managed objects).
                components.Dispose();
            }

            // Release unmanaged resources, set large fields to null.
            CB.ChangeClipboardChain(Handle, _nextCb);
            CB.UnhookWindowsHookEx(_hhook);

            _disposed = true;

            base.Dispose(disposing);
        }
        #endregion



        /// <summary>
        /// Draw me.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {



            //// Setup.
            //pe.Graphics.Clear(BackColor);

            //switch (Ctype)
            //{
            //    case ClipType.PlainText:
            //    case ClipType.RichText:
            //        SizeF stext = pe.Graphics.MeasureString(ShortText, Font);
            //        pe.Graphics.DrawString(ShortText, Font, _defaultForeBrush, ClientRectangle);
            //        break;

            //    case ClipType.Image:
            //        pe.Graphics.DrawImage(Thumbnail, 0, 0);
            //        break;

            //    default:
            //        pe.Graphics.Clear(Color.SpringGreen);
            //        break;
            //}
        }






        #region Clip Management
        /// <summary>
        /// 
        /// </summary>
        void UpdateClipDisplays()
        {
            // Remove tail(s).
            while (_clips.Count > MAX_CLIPS)
            {
                var clip = _clips.Last();
                Controls.Remove(clip);
                _clips.Remove(clip);
            }

            // Fill UI with what we have.
            foreach (var clip in _clips)
            {
                clip.Show();
                switch (clip.Ctype)
                {
                    case ClipType.Empty:
                        clip.SetEmpty();
                        //ds.Visible = Debug;
                        break;

                    case ClipType.Image:
//                        clip.SetImage(clip.Bitmap!, _fitImage);
                        break;

                    case ClipType.Other:
//                        clip.SetOther(clip.Data!.ToString()!);
                        break;

                    case ClipType.PlainText:
                    case ClipType.RichText:
                    //case ClipType.FileList:
                        clip.SetText(clip.Ctype, clip.Text);
                        break;
                }
            }

            //for (int i = 0; i < MAX_CLIPS; i++)
            //{
            //    var ds = _clips[i];
            //    ds.Show();

            //    if (i < _clips.Count)
            //    {
            //        var clip = _clips.ElementAt(i);

            //        switch (clip.Ctype)
            //        {
            //            case ClipType.Empty:
            //                ds.SetEmpty();
            //                //ds.Visible = Debug;
            //                break;

            //            case ClipType.Image:
            //                ds.SetImage(clip.Bitmap!, _fitImage);
            //                break;

            //            case ClipType.Other:
            //                ds.SetOther(clip.Data!.ToString()!);
            //                break;

            //            case ClipType.PlainText:
            //            case ClipType.RichText:
            //            case ClipType.FileList:
            //                ds.SetText(clip.Ctype, clip.Text);
            //                break;
            //        }
            //    }
            //    else
            //    {
            //        ds.SetEmpty();
            //        //ds.Visible = _settings.Debug;
            //    }
            //}
        }
        #endregion

        #region Windows Message Processing - Cut/Copy/Paste
        /// <summary>
        /// Handle window messages.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            int ret = 0;

            if (_clipboardMessages.TryGetValue(m.Msg, out MsgSpec? value))
            {
                MsgSpec sp = value;
                Tell($"WndProc message {sp.Name} HWnd:{m.HWnd} Msg:{m.Msg} WParam:{m.WParam} LParam:{m.LParam} ");
                
                // Call handler.
                ret = sp.Handler(m);
                if (ret > 0)
                {
                    Tell($"ERR WndProc handler {sp.Name} ret:0X{ret:X}");
                }
            }
            else
            {
                // Ignore, pass along.
                base.WndProc(ref m);
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
                        var appName = Path.GetFileName(process.MainModule!.FileName);

                        // Data type info.
                        var dtypes = dobj.GetFormats();
                        var stypes = $"CbDraw dtypes:{string.Join(",", dtypes)}";

                        //Tell($"CbDraw COPY appName:{appName} procName:{procName} title:{info.Title} types:{stypes}");

                        // Determine data type. This is only interested in text and images - all others are passed on to smarter clients.


                        ClipDisplay_1? clip = null;

                        //ClipDisplay_1 clip = new()
                        //{
                        //    Ctype = ClipType.Other,
                        //    Data = Clipboard.GetDataObject(),
                        //    OriginatingApp = appName ?? "Unknown",
                        //    OriginatingTitle = info.Title.ToString()
                        //};

                        if (Clipboard.ContainsText())
                        {
                            // Is it rich text? dtypes: Rich Text Format, Rich Text Format Without Objects, RTF As Text.
                            var ctype = ClipType.PlainText;
                            foreach(var dt in dtypes)
                            {
                                if (dt.Contains("Rich Text Format") || dt.Contains("RTF"))
                                {
                                    ctype = ClipType.RichText;
                                    //Text = Clipboard.GetText().Left(80);
                                    break;
                                }
                            }

                            clip = new()
                            {
                                Ctype = ctype,
                                Text = Clipboard.GetText().Left(80)
                            };
                        }
                        else if (Clipboard.ContainsImage())
                        {
                            var bmp = Clipboard.GetImage() as Bitmap;
                            int tnHeight = Height;
                            int tnWidth = Width * Height / bmp.Width;
                            bmp.Resize(tnWidth, tnHeight);

                            clip = new()
                            {
                                Ctype = ClipType.Image,
                                Thumbnail = bmp // Clipboard.GetImage() as Bitmap // TODO thumbnail
                            };
                        }
                        //else if (Clipboard.ContainsFileDropList())
                        //{
                        //    clip.Ctype = ClipType.FileList;
                        //    clip.Text = string.Join(Environment.NewLine, Clipboard.GetFileDropList());
                        //}
                        //else
                        //{
                        //    // Something else, don't try to show it.
                        //}

                        if (clip != null)
                        {
                            clip.Data = Clipboard.GetDataObject();
                            clip.OriginatingApp = appName ?? "Unknown";
                            clip.OriginatingTitle = info.Title.ToString();
                            clip.Click += (sender, e) => { ClipClick(sender as ClipDisplay_1, true); };
                            clip.DoubleClick += (sender, e) => { ClipClick(sender as ClipDisplay_1, false); };

                            _clips.AddFirst(clip);
                            Controls.Add(clip);

                            // Limit.
                            // Remove tail(s).
                            while (_clips.Count > MAX_CLIPS)
                            {
                                var clipx = _clips.Last();
                                Controls.Remove(clipx);
                                _clips.Remove(clipx);
                            }


                            Invalidate();

//                            UpdateClipDisplays();
                        }
                        else
                        {
                            // Pass along to the next in the chain.
                            ret = W32.SendMessage(_nextCb, m.Msg, m.WParam, m.LParam);
                        }
                        //ClipDisplay_1 clip = new()
                        //{
                        //    Ctype = ClipType.Other,
                        //    Data = Clipboard.GetDataObject(),
                        //    OriginatingApp = appName ?? "Unknown",
                        //    OriginatingTitle = info.Title.ToString()
                        //};

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

        /// <summary>
        /// Process the clipboard change message. Update our link.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        int CbChange(Message m)
        {
            var ret = 0;

            if (m.WParam == _nextCb)
            {
                // Fix our copy of the chain.
                _nextCb = m.LParam;
            }
            else
            {
                // Just pass along to the next in the chain.
                ret = W32.SendMessage(_nextCb, m.Msg, m.WParam, m.LParam);
            }

            return ret;
        }

        /// <summary>
        /// Process all other messages. Doesn't do anything right now other than call base.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        int CbDefault(Message m)
        {
            int ret = 0;
            base.WndProc(ref m);
            return ret;
        }
        #endregion

        #region Paste Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="singleClick"></param>
        void ClipClick(ClipDisplay_1? clip, bool singleClick)
        {
            if (singleClick)
            {
                Tell("!!! Got a single click");
                if (clip is not null && clip.Data is not null)
                {
                    Clipboard.SetDataObject(clip.Data);
                    DoPaste();
                    // Push to head of class.
                    _clips.Remove(clip);
                    _clips.AddFirst(clip);
                }
                else
                {
                    Controls.Remove(clip);
                    _clips.Remove(clip);
                }

                Invalidate();
            }
            else // double
            {
                Tell("!!! Got a double click TODO");
            }
            //  TODO Also could do left, right, delete, etc.
        }

        /// <summary>
        /// Send paste to focus window.
        /// </summary>
        void DoPaste()
        {
            // TODO but the focus is me now!

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

        /// <summary>
        /// Low level hook function. Sniffs for magic key.
        /// </summary>
        /// <param name="code">If less than zero, pass the message to the CallNextHookEx function without further processing.</param>
        /// <param name="wParam">One of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">Pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns></returns>
        int KeyboardHookProc(int code, int wParam, ref CB.KBDLLHOOKSTRUCT lParam)
        {
            if (code >= 0)
            {
                Keys key = (Keys)lParam.vkCode;

                // Tell($"KeyboardHookProc code:{code} wParam:{wParam} key:{key} scancode:{lParam.scanCode}");

                if (code >= 0)
                {
                    // Update statuses.
                    bool pressed = wParam == W32.WM_KEYDOWN || wParam == W32.WM_SYSKEYDOWN;
                    //bool up = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;

                    if (key == Keys.LWin || key == Keys.RWin)
                    {
                        _winPressed = pressed;
                    }

                    if (key == _keyTrigger)
                    {
                        _letterPressed = pressed;
                    }

                    bool match = _winPressed && _letterPressed;

                    // Diagnostics.
                    lblWin.BackColor = _winPressed ? _drawColor : Color.Transparent;
                    lblLetter.BackColor = _letterPressed ? _drawColor : Color.Transparent;
                    lblMatch.BackColor = match ? _drawColor : Color.Transparent;

                    if (match)
                    {
                        // show UI;
                        Visible = true;
                    }
                }
            }

            return CB.CallNextHookEx(_hhook, code, wParam, ref lParam);
        }
        #endregion

        #region Debug
        /// <summary>
        /// Debug stuff.
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
            string s = $"{DateTime.Now:mm\\:ss\\.fff} {msg}";
            tvInfo.Append(s);
        }
        #endregion
    }
}