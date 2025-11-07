using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace EKSE.Components
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
            // 移除了CreateKeyboard()调用，因为键盘现在在XAML中静态定义
        }

        // 按键点击事件
        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string keyName)
            {
                // 将字符串转换为Key枚举
                if (Enum.TryParse<Key>(keyName, out Key key))
                {
                    _selectedKey = key;
                    KeySelected?.Invoke(this, new VirtualKeyEventArgs(key));
                }
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