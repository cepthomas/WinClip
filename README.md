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
    - clip.click => paste selection to last fg window


# stuff

How to Handle Multiple Notifications:

    Debouncing/Debounce/Throttling: Implement a timer (e.g., 50-100ms) to wait for the clipboard to "settle" before processing the data, ensuring you react only to the final change.
    Check Sequence Numbers: Use GetClipboardSequenceNumber to determine if the actual content has changed, rather than relying on the message itself.

The usual mitigation strategy is to avoid reacting to every update, and react to the LAST update after a reasonable "settle time" has elapsed with no further clipboard notifications. 500ms will usually be more than adequate.

