# WinClip

Yet another clipboard manager.

Very much a WIP...


# Notes
Defaults:
- Win+V - Open Clipboard bin.


- Standard copy op ctrl-C:
    - System puts content in standard clipboard.
    - App also puts content at top of app fifo (front of list).
- Standard paste op ctrl-V:
    - Works as usual; app doesn't do anything.

- MagicKey
    - make app window visible
    - clip.click => 

c

# Notez

>>> start
2026-04-22 10:45:40.008 : DBG APP MainForm.cs(112) Myself init ProcessInfo::: hwnd:[0X00110B00] procName:[WinClip] appName:[WinClip.exe] title:[Hoo Haa]

>>> key press in WinClip
2026-04-22 10:45:43.195 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00110B00] procName:[WinClip] appName:[WinClip.exe] title:[Hoo Haa]
2026-04-22 10:45:43.197 : DBG APP MainForm.cs(394) KeyboardHookProc key:A scanCode:30 FG:[0X00110B00]
2026-04-22 10:45:43.393 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00110B00] procName:[WinClip] appName:[WinClip.exe] title:[Hoo Haa]


>>> key press in VS
2026-04-22 10:45:47.351 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X009C0288] procName:[devenv] appName:[devenv.exe] title:[WinClip (Running) - MainForm.cs - Microsoft Visual Studio]
2026-04-22 10:45:47.351 : DBG APP MainForm.cs(394) KeyboardHookProc key:B scanCode:48 FG:[0X009C0288]
2026-04-22 10:45:47.491 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X009C0288] procName:[devenv] appName:[devenv.exe] title:[WinClip (Running) - MainForm.cs* - Microsoft Visual Studio]


>>> key press in Sublime
2026-04-22 10:45:54.398 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Dev\Apps\WinClip\README.md (winapps) - Sublime Text]
2026-04-22 10:45:54.398 : DBG APP MainForm.cs(394) KeyboardHookProc key:C scanCode:46 FG:[0X00020792]
2026-04-22 10:45:54.556 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Dev\Apps\WinClip\README.md • (winapps) - Sublime Text]

>>> ctrl-C in sublime
2026-04-22 10:46:00.728 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Dev\Apps\WinClip\README.md • (winapps) - Sublime Text]
2026-04-22 10:46:00.728 : DBG APP MainForm.cs(394) KeyboardHookProc key:LControlKey scanCode:29 FG:[0X00020792]

2026-04-22 10:46:01.547 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Dev\Apps\WinClip\README.md • (winapps) - Sublime Text]
2026-04-22 10:46:01.547 : DBG APP MainForm.cs(394) KeyboardHookProc key:C scanCode:46 FG:[0X00020792]

2026-04-22 10:46:01.550 : DBG APP MainForm.cs(198) UpdateClipboard() HWnd:0X00110B00 Msg:797 WParam:5 LParam:0
2026-04-22 10:46:01.553 : DBG APP MainForm.cs(210) WndProc WM_CLIPBOARDUPDATE ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Dev\Apps\WinClip\README.md • (winapps) - Sublime Text]
2026-04-22 10:46:01.559 : DBG APP MainForm.cs(285) New clip:Clip 1 PlainText

>>> ???
2026-04-22 10:46:01.645 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Dev\Apps\WinClip\README.md • (winapps) - Sublime Text]
2026-04-22 10:46:01.645 : DBG APP MainForm.cs(394) KeyboardHookProc key:C scanCode:46 FG:[0X00020792]

2026-04-22 10:46:02.433 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Dev\Apps\WinClip\README.md • (winapps) - Sublime Text]
2026-04-22 10:46:02.433 : DBG APP MainForm.cs(394) KeyboardHookProc key:LControlKey scanCode:29 FG:[0X00020792]

2026-04-22 10:46:28.331 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Users\cepth\AppData\Roaming\Ephemera\WinClip\log.txt (winapps) - Sublime Text]
2026-04-22 10:46:28.331 : DBG APP MainForm.cs(394) KeyboardHookProc key:LControlKey scanCode:29 FG:[0X00020792]

2026-04-22 10:46:28.572 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Users\cepth\AppData\Roaming\Ephemera\WinClip\log.txt (winapps) - Sublime Text]
2026-04-22 10:46:28.572 : DBG APP MainForm.cs(394) KeyboardHookProc key:LShiftKey scanCode:42 FG:[0X00020792]

2026-04-22 10:46:29.300 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Users\cepth\AppData\Roaming\Ephemera\WinClip\log.txt (winapps) - Sublime Text]
2026-04-22 10:46:29.300 : DBG APP MainForm.cs(394) KeyboardHookProc key:W scanCode:17 FG:[0X00020792]

2026-04-22 10:46:29.423 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Users\cepth\AppData\Roaming\Ephemera\WinClip\log.txt (winapps) - Sublime Text]
2026-04-22 10:46:29.423 : DBG APP MainForm.cs(394) KeyboardHookProc key:W scanCode:17 FG:[0X00020792]

2026-04-22 10:46:30.180 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Users\cepth\AppData\Roaming\Ephemera\WinClip\log.txt (winapps) - Sublime Text]
2026-04-22 10:46:30.180 : DBG APP MainForm.cs(394) KeyboardHookProc key:LShiftKey scanCode:42 FG:[0X00020792]

2026-04-22 10:46:30.184 : DBG APP MainForm.cs(390) KeyboardHookProc ProcessInfo::: hwnd:[0X00020792] procName:[sublime_text] appName:[sublime_text.exe] title:[C:\Users\cepth\AppData\Roaming\Ephemera\WinClip\log.txt (winapps) - Sublime Text]

2026-04-22 10:46:30.184 : DBG APP MainForm.cs(394) KeyboardHookProc key:LControlKey scanCode:29 FG:[0X00020792]

>>> click clip
2026-04-22 10:47:02.726 : DBG APP MainForm.cs(198) UpdateClipboard() HWnd:0X00110B00 Msg:797 WParam:7 LParam:0
2026-04-22 10:47:02.729 : DBG APP MainForm.cs(210) WndProc WM_CLIPBOARDUPDATE ProcessInfo::: hwnd:[0X00110B00] procName:[WinClip] appName:[WinClip.exe] title:[Hoo Haa]
2026-04-22 10:47:02.952 : DBG APP MainForm.cs(285) New clip:Clip 2 PlainText
