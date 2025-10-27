using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KeySound2;

public partial class VirtualKeyboard : UserControl
{
    public event EventHandler<KeyPressedEventArgs> OnKeyPressed;
    
    private Dictionary<Key, Button> _keyButtons = new Dictionary<Key, Button>();
    private Dictionary<Button, Brush> _originalBackgrounds = new Dictionary<Button, Brush>();
    
    public VirtualKeyboard()
    {
        InitializeComponent();
        MapKeyButtons();
    }
    
    private void MapKeyButtons()
    {
        // 映射所有按键按钮到对应的Key值
        _keyButtons[Key.Escape] = EscKey;
        _keyButtons[Key.F1] = F1Key;
        _keyButtons[Key.F2] = F2Key;
        _keyButtons[Key.F3] = F3Key;
        _keyButtons[Key.F4] = F4Key;
        _keyButtons[Key.F5] = F5Key;
        _keyButtons[Key.F6] = F6Key;
        _keyButtons[Key.F7] = F7Key;
        _keyButtons[Key.F8] = F8Key;
        _keyButtons[Key.F9] = F9Key;
        _keyButtons[Key.F10] = F10Key;
        _keyButtons[Key.F11] = F11Key;
        _keyButtons[Key.F12] = F12Key;
        
        _keyButtons[Key.Oem3] = TildeKey; // ~ `
        _keyButtons[Key.D1] = Num1Key;
        _keyButtons[Key.D2] = Num2Key;
        _keyButtons[Key.D3] = Num3Key;
        _keyButtons[Key.D4] = Num4Key;
        _keyButtons[Key.D5] = Num5Key;
        _keyButtons[Key.D6] = Num6Key;
        _keyButtons[Key.D7] = Num7Key;
        _keyButtons[Key.D8] = Num8Key;
        _keyButtons[Key.D9] = Num9Key;
        _keyButtons[Key.D0] = Num0Key;
        _keyButtons[Key.OemMinus] = MinusKey; // - _
        _keyButtons[Key.OemPlus] = EqualsKey; // = +
        _keyButtons[Key.Back] = BackspaceKey;
        
        _keyButtons[Key.Tab] = TabKey;
        _keyButtons[Key.Q] = QKey;
        _keyButtons[Key.W] = WKey;
        _keyButtons[Key.E] = EKey;
        _keyButtons[Key.R] = RKey;
        _keyButtons[Key.T] = TKey;
        _keyButtons[Key.Y] = YKey;
        _keyButtons[Key.U] = UKey;
        _keyButtons[Key.I] = IKey;
        _keyButtons[Key.O] = OKey;
        _keyButtons[Key.P] = PKey;
        _keyButtons[Key.OemOpenBrackets] = OpenBracketKey; // [ {
        _keyButtons[Key.OemCloseBrackets] = CloseBracketKey; // ] }
        _keyButtons[Key.Oem5] = BackslashKey; // \ |
        
        _keyButtons[Key.CapsLock] = CapsLockKey;
        _keyButtons[Key.A] = AKey;
        _keyButtons[Key.S] = SKey;
        _keyButtons[Key.D] = DKey;
        _keyButtons[Key.F] = FKey;
        _keyButtons[Key.G] = GKey;
        _keyButtons[Key.H] = HKey;
        _keyButtons[Key.J] = JKey;
        _keyButtons[Key.K] = KKey;
        _keyButtons[Key.L] = LKey;
        _keyButtons[Key.OemSemicolon] = SemicolonKey; // ; :
        _keyButtons[Key.OemQuotes] = QuoteKey; // ' "
        _keyButtons[Key.Enter] = EnterKey;
        
        _keyButtons[Key.LeftShift] = ShiftKey;
        _keyButtons[Key.Z] = ZKey;
        _keyButtons[Key.X] = XKey;
        _keyButtons[Key.C] = CKey;
        _keyButtons[Key.V] = VKey;
        _keyButtons[Key.B] = BKey;
        _keyButtons[Key.N] = NKey;
        _keyButtons[Key.M] = MKey;
        _keyButtons[Key.OemComma] = CommaKey; // , <
        _keyButtons[Key.OemPeriod] = PeriodKey; // . >
        _keyButtons[Key.OemQuestion] = SlashKey; // / ?
        _keyButtons[Key.RightShift] = RightShiftKey;
        
        _keyButtons[Key.LeftCtrl] = CtrlKey;
        _keyButtons[Key.LWin] = WinKey;
        _keyButtons[Key.LeftAlt] = AltKey;
        _keyButtons[Key.Space] = SpaceKey;
        _keyButtons[Key.RightAlt] = RightAltKey;
        _keyButtons[Key.RWin] = RightWinKey;
        _keyButtons[Key.Apps] = MenuKey;
        _keyButtons[Key.RightCtrl] = RightCtrlKey;
        
        // 方向键
        _keyButtons[Key.Up] = UpKey;
        _keyButtons[Key.Down] = DownKey;
        _keyButtons[Key.Left] = LeftKey;
        _keyButtons[Key.Right] = RightKey;
        
        // 小键盘
        _keyButtons[Key.NumLock] = NumLockKey;
        _keyButtons[Key.Divide] = NumpadDivideKey;
        _keyButtons[Key.Multiply] = NumpadMultiplyKey;
        _keyButtons[Key.Subtract] = NumpadMinusKey;
        _keyButtons[Key.Add] = NumpadPlusKey;
        _keyButtons[Key.NumPad0] = Numpad0Key;
        _keyButtons[Key.NumPad1] = Numpad1Key;
        _keyButtons[Key.NumPad2] = Numpad2Key;
        _keyButtons[Key.NumPad3] = Numpad3Key;
        _keyButtons[Key.NumPad4] = Numpad4Key;
        _keyButtons[Key.NumPad5] = Numpad5Key;
        _keyButtons[Key.NumPad6] = Numpad6Key;
        _keyButtons[Key.NumPad7] = Numpad7Key;
        _keyButtons[Key.NumPad8] = Numpad8Key;
        _keyButtons[Key.NumPad9] = Numpad9Key;
        _keyButtons[Key.Decimal] = NumpadDotKey;
        _keyButtons[Key.Return] = NumpadEnterKey; // 小键盘回车
        
        // 为所有按钮添加事件处理
        foreach (var kvp in _keyButtons)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Tag = kvp.Key;
                kvp.Value.Click += KeyButton_Click;
                kvp.Value.PreviewMouseDown += KeyButton_PreviewMouseDown;
                kvp.Value.PreviewMouseUp += KeyButton_PreviewMouseUp;
                
                _originalBackgrounds[kvp.Value] = kvp.Value.Background;
            }
        }
    }
    
    private void KeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Key key)
        {
            OnKeyPressed?.Invoke(this, new KeyPressedEventArgs(key));
        }
    }
    
    private void KeyButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button button)
        {
            // 不再修改Tag属性，只改变背景色
            button.Background = new SolidColorBrush(Colors.LightBlue);
        }
    }
    
    private void KeyButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button button)
        {
            // 恢复按钮的原始背景色
            if (_originalBackgrounds.ContainsKey(button))
            {
                button.Background = _originalBackgrounds[button];
            }
            else
            {
                // 如果找不到原始背景色，则使用默认背景色
                button.Background = new SolidColorBrush(Colors.LightGray);
            }
        }
    }
}