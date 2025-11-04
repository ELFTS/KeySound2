using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace KeySound2.Components
{
    /// <summary>
    /// VirtualKeyboard.xaml 的交互逻辑
    /// </summary>
    public partial class VirtualKeyboard : UserControl
    {
        // 定义按键事件
        public event EventHandler<VirtualKeyEventArgs> KeySelected;
        
        // 存储按键到音效路径的映射
        private Dictionary<Key, string> _keySoundMap;
        
        // 当前选中的按键
        private Key _selectedKey;

        public VirtualKeyboard()
        {
            InitializeComponent();
            _keySoundMap = new Dictionary<Key, string>();
            CreateKeyboard();
        }

        // 创建虚拟键盘
        private void CreateKeyboard()
        {
            // 清空现有内容
            KeyboardGrid.Children.Clear();
            KeyboardGrid.RowDefinitions.Clear();
            KeyboardGrid.ColumnDefinitions.Clear();

            // 定义键盘行
            var keyboardRows = new List<List<KeyInfo>>
            {
                // 第一行: ESC, F1-F12
                new List<KeyInfo>
                {
                    new KeyInfo { Key = Key.Escape, Text = "ESC", Width = 60 },
                    new KeyInfo { Key = Key.None, Text = "", Width = 10 }, // 空隙
                    new KeyInfo { Key = Key.F1, Text = "F1", Width = 40 },
                    new KeyInfo { Key = Key.F2, Text = "F2", Width = 40 },
                    new KeyInfo { Key = Key.F3, Text = "F3", Width = 40 },
                    new KeyInfo { Key = Key.F4, Text = "F4", Width = 40 },
                    new KeyInfo { Key = Key.None, Text = "", Width = 10 }, // 空隙
                    new KeyInfo { Key = Key.F5, Text = "F5", Width = 40 },
                    new KeyInfo { Key = Key.F6, Text = "F6", Width = 40 },
                    new KeyInfo { Key = Key.F7, Text = "F7", Width = 40 },
                    new KeyInfo { Key = Key.F8, Text = "F8", Width = 40 },
                    new KeyInfo { Key = Key.None, Text = "", Width = 10 }, // 空隙
                    new KeyInfo { Key = Key.F9, Text = "F9", Width = 40 },
                    new KeyInfo { Key = Key.F10, Text = "F10", Width = 40 },
                    new KeyInfo { Key = Key.F11, Text = "F11", Width = 40 },
                    new KeyInfo { Key = Key.F12, Text = "F12", Width = 40 }
                },
                
                // 第二行: 波浪号, 1-0, -, =, Backspace
                new List<KeyInfo>
                {
                    new KeyInfo { Key = Key.Oem3, Text = "~\n`", Width = 40 },
                    new KeyInfo { Key = Key.D1, Text = "!\n1", Width = 40 },
                    new KeyInfo { Key = Key.D2, Text = "@\n2", Width = 40 },
                    new KeyInfo { Key = Key.D3, Text = "#\n3", Width = 40 },
                    new KeyInfo { Key = Key.D4, Text = "$\n4", Width = 40 },
                    new KeyInfo { Key = Key.D5, Text = "%\n5", Width = 40 },
                    new KeyInfo { Key = Key.D6, Text = "^\n6", Width = 40 },
                    new KeyInfo { Key = Key.D7, Text = "&\n7", Width = 40 },
                    new KeyInfo { Key = Key.D8, Text = "*\n8", Width = 40 },
                    new KeyInfo { Key = Key.D9, Text = "(\n9", Width = 40 },
                    new KeyInfo { Key = Key.D0, Text = ")\n0", Width = 40 },
                    new KeyInfo { Key = Key.OemMinus, Text = "_\n-", Width = 40 },
                    new KeyInfo { Key = Key.OemPlus, Text = "+\n=", Width = 40 },
                    new KeyInfo { Key = Key.Back, Text = "Backspace", Width = 80 }
                },
                
                // 第三行: Tab, Q-P, [, ], \
                new List<KeyInfo>
                {
                    new KeyInfo { Key = Key.Tab, Text = "Tab", Width = 60 },
                    new KeyInfo { Key = Key.Q, Text = "Q", Width = 40 },
                    new KeyInfo { Key = Key.W, Text = "W", Width = 40 },
                    new KeyInfo { Key = Key.E, Text = "E", Width = 40 },
                    new KeyInfo { Key = Key.R, Text = "R", Width = 40 },
                    new KeyInfo { Key = Key.T, Text = "T", Width = 40 },
                    new KeyInfo { Key = Key.Y, Text = "Y", Width = 40 },
                    new KeyInfo { Key = Key.U, Text = "U", Width = 40 },
                    new KeyInfo { Key = Key.I, Text = "I", Width = 40 },
                    new KeyInfo { Key = Key.O, Text = "O", Width = 40 },
                    new KeyInfo { Key = Key.P, Text = "P", Width = 40 },
                    new KeyInfo { Key = Key.OemOpenBrackets, Text = "{\n[", Width = 40 },
                    new KeyInfo { Key = Key.OemCloseBrackets, Text = "}\n]", Width = 40 },
                    new KeyInfo { Key = Key.Oem5, Text = "|\n\\", Width = 60 }
                },
                
                // 第四行: Caps Lock, A-L, ;, ', Enter
                new List<KeyInfo>
                {
                    new KeyInfo { Key = Key.CapsLock, Text = "Caps Lock", Width = 70 },
                    new KeyInfo { Key = Key.A, Text = "A", Width = 40 },
                    new KeyInfo { Key = Key.S, Text = "S", Width = 40 },
                    new KeyInfo { Key = Key.D, Text = "D", Width = 40 },
                    new KeyInfo { Key = Key.F, Text = "F", Width = 40 },
                    new KeyInfo { Key = Key.G, Text = "G", Width = 40 },
                    new KeyInfo { Key = Key.H, Text = "H", Width = 40 },
                    new KeyInfo { Key = Key.J, Text = "J", Width = 40 },
                    new KeyInfo { Key = Key.K, Text = "K", Width = 40 },
                    new KeyInfo { Key = Key.L, Text = "L", Width = 40 },
                    new KeyInfo { Key = Key.OemSemicolon, Text = ":\n;", Width = 40 },
                    new KeyInfo { Key = Key.OemQuotes, Text = "\"\n'", Width = 40 },
                    new KeyInfo { Key = Key.Enter, Text = "Enter", Width = 90 }
                },
                
                // 第五行: Shift, Z-M, ,, ., /, Shift
                new List<KeyInfo>
                {
                    new KeyInfo { Key = Key.LeftShift, Text = "Shift", Width = 90 },
                    new KeyInfo { Key = Key.Z, Text = "Z", Width = 40 },
                    new KeyInfo { Key = Key.X, Text = "X", Width = 40 },
                    new KeyInfo { Key = Key.C, Text = "C", Width = 40 },
                    new KeyInfo { Key = Key.V, Text = "V", Width = 40 },
                    new KeyInfo { Key = Key.B, Text = "B", Width = 40 },
                    new KeyInfo { Key = Key.N, Text = "N", Width = 40 },
                    new KeyInfo { Key = Key.M, Text = "M", Width = 40 },
                    new KeyInfo { Key = Key.OemComma, Text = "<\n,", Width = 40 },
                    new KeyInfo { Key = Key.OemPeriod, Text = ">\n.", Width = 40 },
                    new KeyInfo { Key = Key.OemQuestion, Text = "?\n/", Width = 40 },
                    new KeyInfo { Key = Key.RightShift, Text = "Shift", Width = 110 }
                },
                
                // 第六行: Ctrl, Win, Alt, Space, Alt, Win, Menu, Ctrl
                new List<KeyInfo>
                {
                    new KeyInfo { Key = Key.LeftCtrl, Text = "Ctrl", Width = 60 },
                    new KeyInfo { Key = Key.LWin, Text = "Win", Width = 60 },
                    new KeyInfo { Key = Key.LeftAlt, Text = "Alt", Width = 60 },
                    new KeyInfo { Key = Key.Space, Text = "Space", Width = 250 },
                    new KeyInfo { Key = Key.RightAlt, Text = "Alt", Width = 60 },
                    new KeyInfo { Key = Key.RWin, Text = "Win", Width = 60 },
                    new KeyInfo { Key = Key.Apps, Text = "Menu", Width = 60 },
                    new KeyInfo { Key = Key.RightCtrl, Text = "Ctrl", Width = 60 }
                }
            };

            // 创建行
            for (int i = 0; i < keyboardRows.Count; i++)
            {
                KeyboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // 创建按键
            for (int row = 0; row < keyboardRows.Count; row++)
            {
                var rowKeys = keyboardRows[row];
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                foreach (var keyInfo in rowKeys)
                {
                    if (keyInfo.Key == Key.None)
                    {
                        // 添加空隙
                        var spacer = new Border { Width = keyInfo.Width };
                        stackPanel.Children.Add(spacer);
                    }
                    else
                    {
                        // 创建按键按钮
                        var button = new Button
                        {
                            Content = keyInfo.Text,
                            Width = keyInfo.Width,
                            Height = 40,
                            Margin = new Thickness(2),
                            Tag = keyInfo.Key
                        };

                        // 设置按钮样式
                        button.Style = FindResource("MaterialDesignFlatButton") as Style;
                        
                        // 绑定点击事件
                        button.Click += KeyButton_Click;
                        
                        stackPanel.Children.Add(button);
                    }
                }

                // 将行添加到网格
                Grid.SetRow(stackPanel, row);
                KeyboardGrid.Children.Add(stackPanel);
            }
        }

        // 按键点击事件
        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Key key)
            {
                _selectedKey = key;
                KeySelected?.Invoke(this, new VirtualKeyEventArgs(key));
            }
        }

        // 设置按键的音效路径
        public void SetKeySound(Key key, string soundPath)
        {
            _keySoundMap[key] = soundPath;
        }

        // 获取按键的音效路径
        public string GetKeySound(Key key)
        {
            return _keySoundMap.ContainsKey(key) ? _keySoundMap[key] : null;
        }

        // 获取当前选中的按键
        public Key SelectedKey => _selectedKey;
    }

    // 按键信息类
    public class KeyInfo
    {
        public Key Key { get; set; }
        public string Text { get; set; }
        public double Width { get; set; }
    }

    // 按键事件参数类
    public class VirtualKeyEventArgs : EventArgs
    {
        public Key Key { get; }

        public VirtualKeyEventArgs(Key key)
        {
            Key = key;
        }
    }
}