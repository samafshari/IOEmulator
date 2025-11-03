using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Neat;

public enum KeyEventType { Down, Up }

// Subset of keys; extend as needed
public enum KeyCode
{
    Unknown = 0,
    // ASCII printable will be represented via Char on the event
    Enter, Backspace, Tab, Escape,
    Left, Right, Up, Down,
    Home, End, PageUp, PageDown,
    Insert, Delete,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
}

public readonly struct KeyEvent
{
    public readonly KeyEventType Type;
    public readonly KeyCode Code;
    public readonly char? Char; // when printable
    public readonly bool Shift, Ctrl, Alt;

    public KeyEvent(KeyEventType type, KeyCode code, char? ch = null, bool shift = false, bool ctrl = false, bool alt = false)
    {
        Type = type; Code = code; Char = ch; Shift = shift; Ctrl = ctrl; Alt = alt;
    }
}

public delegate KeyEvent? ReadKeyDelegate();
