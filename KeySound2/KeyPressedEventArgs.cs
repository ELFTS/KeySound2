using System;
using System.Windows.Input;

namespace KeySound2;

public class KeyPressedEventArgs : EventArgs
{
    public Key Key { get; }
    
    public KeyPressedEventArgs(Key key)
    {
        Key = key;
    }
}