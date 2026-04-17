using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Ephemera.NBagOfTricks;


namespace WinClip
{
    /// <summary>Native win32 interop. TODOX => Win32 clipboard stuff</summary>
    public static class Native
    {
        [Flags]
        public enum KBDLLHOOKSTRUCTFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;    // A virtual-key code in the range 1 to 254.
            public uint scanCode;  // A hardware scan code for the key.
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        /// <summary> https://www.pinvoke.net/default.aspx/Enums/HookType.html </summary>
        // Global hooks are not supported in the.NET Framework except for WH_KEYBOARD_LL and WH_MOUSE_LL.
        public enum HookType : int
        {
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        /// <summary>Defines the callback for the hook. Apparently you can have multiple typed overloads.</summary>
        public delegate int HookProc(int code, int wParam, ref KBDLLHOOKSTRUCT lParam);


        [DllImport("User32.dll")]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("User32.dll")] //, CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}