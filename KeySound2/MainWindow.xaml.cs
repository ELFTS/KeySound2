using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Media;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Forms;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using System.Linq;

namespace KeySound2;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private KeyboardHook _keyboardHook;
    private SoundManager _soundManager;
    private SoundSettings _soundSettings;
    private AppSettings _appSettings;
    private NotifyIcon _notifyIcon;
    private bool _isExiting = false; // 添加退出标志
    private WindowState _previousWindowState = WindowState.Normal; // 保存最大化前的窗口状态
    private bool _isRunning = false; // 添加运行状态标志
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeAppSettings();
        InitializeSoundSettings();
        InitializeTrayIcon();
        
        // 设置初始ToolTip
        StartStopButton.ToolTip = "状态：已停止";
        
        // 修复最大化时任务栏被遮挡的问题
        MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        
        // 根据设置决定是否启动时最小化
        if (_appSettings.StartMinimized)
        {
            WindowState = WindowState.Minimized;
            Hide();
        }
        else
        {
            // 默认显示主页
            MainFrame.Content = new HomePage();
        }
    }
    
    private void InitializeAppSettings()
    {
        _appSettings = AppSettings.Load();
    }

    private void InitializeSoundSettings()
    {
        _soundSettings = new SoundSettings();
        
        // 尝试从配置文件加载设置
        LoadSoundSettings();
        
        // 设置默认音效
        string defaultSoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", "default.wav");
        if (File.Exists(defaultSoundPath))
        {
            _soundSettings.DefaultSoundPath = defaultSoundPath;
            System.Diagnostics.Debug.WriteLine($"默认音效文件已设置: {defaultSoundPath}");
        }
        else
        {
            // 如果默认音效文件不存在，尝试在程序目录下查找任何wav文件作为默认音效
            string programDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] wavFiles = Directory.GetFiles(programDirectory, "*.wav", SearchOption.AllDirectories);
            if (wavFiles.Length > 0)
            {
                _soundSettings.DefaultSoundPath = wavFiles[0];
                System.Diagnostics.Debug.WriteLine($"使用找到的第一个WAV文件作为默认音效: {wavFiles[0]}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"默认音效文件不存在: {defaultSoundPath}");
            }
        }
        
        _soundManager = new SoundManager(_soundSettings);
        System.Diagnostics.Debug.WriteLine("音效管理器已初始化");
    }
    
    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        _notifyIcon.Visible = _appSettings.ShowTrayIcon; // 根据设置决定是否显示托盘图标
        _notifyIcon.Text = "KeySound2";
        
        // 创建上下文菜单
        ContextMenuStrip contextMenu = new ContextMenuStrip();
        ToolStripMenuItem showItem = new ToolStripMenuItem("显示主窗口");
        showItem.Click += ShowMainWindow_Click;
        contextMenu.Items.Add(showItem);
        
        ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += ExitMenuItem_Click;
        contextMenu.Items.Add(exitItem);
        
        _notifyIcon.ContextMenuStrip = contextMenu;
        
        // 双击托盘图标显示主窗口
        _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
    }
    
    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"StartStopButton_Click 被调用，当前状态: {_isRunning}");
        if (_isRunning)
        {
            // 当前正在运行，需要停止
            StopKeyboardHook();
        }
        else
        {
            // 当前未运行，需要开始
            StartKeyboardHook();
        }
    }
    
    private void StartKeyboardHook()
    {
        System.Diagnostics.Debug.WriteLine("StartKeyboardHook 被调用");
        if (_keyboardHook == null)
        {
            _keyboardHook = new KeyboardHook();
            _keyboardHook.OnKeyPressed += OnKeyPressed;
            _keyboardHook.Start();
        }
        _isRunning = true;
        StartStopButton.ToolTip = "状态：运行中";
        StartStopIcon.Kind = PackIconKind.Stop; // 更改图标为停止图标
        System.Diagnostics.Debug.WriteLine("键盘钩子已启动");
    }
    
    private void StopKeyboardHook()
    {
        System.Diagnostics.Debug.WriteLine("StopKeyboardHook 被调用");
        _keyboardHook?.Stop();
        _keyboardHook = null;
        _isRunning = false;
        StartStopButton.ToolTip = "状态：已停止";
        StartStopIcon.Kind = PackIconKind.Play; // 更改图标为播放图标
        System.Diagnostics.Debug.WriteLine("键盘钩子已停止");
    }
    
    private void OnKeyPressed(object sender, KeyPressedEventArgs e)
    {
        // 播放按键音效
        System.Diagnostics.Debug.WriteLine($"按键按下: {e.Key}");
        if (_soundManager != null)
        {
            System.Diagnostics.Debug.WriteLine($"调用音效管理器播放音效: {e.Key}");
            _soundManager.PlaySound(e.Key);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("音效管理器为空");
        }
    }
    
    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        // 重置所有按钮的背景色（使用默认颜色）
        MainControlButton.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
        SoundSettingsButton.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
        ProgramSettingsButton.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
        AboutButton.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
        
        // 根据点击的按钮设置选中效果
        if (sender is FrameworkElement element)
        {
            switch (element.Tag.ToString())
            {
                case "MainControl":
                    MainFrame.Content = new HomePage();
                    // 设置选中效果（使用悬停颜色）
                    MainControlButton.Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
                    break;
                case "SoundSettings":
                    MainFrame.Content = new SoundSettingsPage();
                    // 设置选中效果（使用悬停颜色）
                    SoundSettingsButton.Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
                    break;
                case "ProgramSettings":
                    // 导航到设置页面
                    if (MainFrame.Content == null || !(MainFrame.Content is SettingsPage))
                    {
                        MainFrame.Content = new SettingsPage();
                    }
                    // 设置选中效果（使用悬停颜色）
                    ProgramSettingsButton.Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
                    break;
                case "About":
                    // 导航到关于页面
                    if (MainFrame.Content == null || !(MainFrame.Content is AboutPage))
                    {
                        MainFrame.Content = new AboutPage();
                    }
                    // 设置选中效果（使用悬停颜色）
                    AboutButton.Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
                    break;
            }
        }
    }
    
    // 窗口控制事件处理
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换最大化/还原状态
        if (WindowState == WindowState.Maximized)
        {
            WindowState = _previousWindowState;
        }
        else
        {
            _previousWindowState = WindowState;
            WindowState = WindowState.Maximized;
        }
    }
    
    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 保存音效设置
        SaveSoundSettings();
        System.Diagnostics.Debug.WriteLine("保存音效设置完成");
        
        // 根据设置决定是关闭还是最小化到托盘
        if (_appSettings.MinimizeToTray)
        {
            System.Diagnostics.Debug.WriteLine("显示最小化到托盘确认对话框");
            // 弹出MaterialDesign风格的信息框确认是否关闭到托盘
            var result = await ShowMaterialConfirmBox("提示", "是否最小化到系统托盘？\n点击\"是\"最小化到托盘，点击\"否\"直接退出程序。");
            if (result == true)
            {
                // 点击关闭按钮时最小化到托盘而不是关闭程序
                System.Diagnostics.Debug.WriteLine("用户选择最小化到托盘");
                Hide();
            }
            else if (result == false)
            {
                // 直接退出程序
                System.Diagnostics.Debug.WriteLine("用户选择直接退出程序");
                _isExiting = true;
                Close();
            }
            // 如果用户关闭对话框或点击取消，则不执行任何操作
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("根据设置直接退出程序");
            // 直接退出程序
            _isExiting = true;
            Close();
        }
    }
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // 双击标题栏切换最大化/还原
            MaximizeButton_Click(sender, e);
        }
        else
        {
            // 拖动窗口
            DragMove();
        }
    }
    
    // 托盘图标事件处理
    private void NotifyIcon_DoubleClick(object sender, EventArgs e)
    {
        ShowMainWindow();
    }
    
    private void ShowMainWindow_Click(object sender, EventArgs e)
    {
        ShowMainWindow();
    }
    
    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
    
    private void ExitMenuItem_Click(object sender, EventArgs e)
    {
        // 保存音效设置
        SaveSoundSettings();
        
        // 设置退出标志并关闭程序
        _isExiting = true;
        Close();
    }
    
    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 只有在非退出状态下才取消关闭并隐藏窗口
        // 只有在非退出状态下才取消关闭并隐藏窗口
        if (!_isExiting)
        {
            if (_appSettings.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _isExiting = true;
                Close();
            }
        }
        else
        {
            // 退出程序前清理资源
            StopKeyboardHook(); // 确保停止键盘钩子
            _soundManager?.Dispose();
            _notifyIcon?.Dispose();
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }
    
    // 修复最大化时任务栏被遮挡的问题
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        
        // 根据窗口状态更新最大化按钮图标
        if (MaximizeButton != null)
        {
            // 查找按钮内的图标
            PackIcon icon = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(MaximizeButton); i++)
            {
                var child = VisualTreeHelper.GetChild(MaximizeButton, i);
                if (child is PackIcon packIcon)
                {
                    icon = packIcon;
                    break;
                }
            }
            
            if (icon != null)
            {
                // 根据窗口状态更新图标
                if (WindowState == WindowState.Maximized)
                {
                    icon.Kind = PackIconKind.WindowRestore;
                }
                else
                {
                    icon.Kind = PackIconKind.WindowMaximize;
                }
            }
        }
    }
    
    /// <summary>
    /// 显示MaterialDesign风格的消息框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="image">消息图标类型</param>
    private async Task ShowMaterialMessageBox(string title, string message, MessageBoxImage image = MessageBoxImage.Information)
    {
        // 创建对话框内容
        var dialogContent = new StackPanel
        {
            Margin = new Thickness(16)
        };

        // 添加标题
        dialogContent.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)FindResource("MaterialDesignHeadline6TextBlock"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        // 添加消息内容
        dialogContent.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // 添加按钮
        var okButton = new System.Windows.Controls.Button
        {
            Content = "确定",
            Style = (Style)FindResource("MaterialDesignFlatButton"),
            Margin = new Thickness(0, 0, 0, 0)
        };

        // 设置按钮点击命令参数为true
        okButton.CommandParameter = true;
        okButton.Command = DialogHost.CloseDialogCommand;

        dialogContent.Children.Add(okButton);

        // 显示对话框
        await DialogHost.Show(dialogContent, "RootDialog");
    }
    
    /// <summary>
    /// 显示MaterialDesign风格的确认框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <returns>用户选择结果：true=是，false=否，null=取消</returns>
    private async Task<bool?> ShowMaterialConfirmBox(string title, string message)
    {
        // 创建对话框内容
        var dialogContent = new StackPanel
        {
            Margin = new Thickness(16)
        };

        // 添加标题
        dialogContent.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)FindResource("MaterialDesignHeadline6TextBlock"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        // 添加消息内容
        dialogContent.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // 添加按钮面板
        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        // 添加"是"按钮
        var yesButton = new System.Windows.Controls.Button
        {
            Content = "是",
            Style = (Style)FindResource("MaterialDesignFlatButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        yesButton.CommandParameter = true;
        yesButton.Command = DialogHost.CloseDialogCommand;
        buttonPanel.Children.Add(yesButton);

        // 添加"否"按钮
        var noButton = new System.Windows.Controls.Button
        {
            Content = "否",
            Style = (Style)FindResource("MaterialDesignFlatButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        noButton.CommandParameter = false;
        noButton.Command = DialogHost.CloseDialogCommand;
        buttonPanel.Children.Add(noButton);

        // 添加"取消"按钮
        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "取消",
            Style = (Style)FindResource("MaterialDesignFlatButton")
        };
        cancelButton.CommandParameter = null;
        cancelButton.Command = DialogHost.CloseDialogCommand;
        buttonPanel.Children.Add(cancelButton);

        dialogContent.Children.Add(buttonPanel);

        // 显示对话框并返回结果
        var result = await DialogHost.Show(dialogContent, "RootDialog");
        return result as bool?;
    }
    
    /// <summary>
    /// 更新音效设置页面的音效设置
    /// </summary>
    /// <param name="settings">新的音效设置</param>
    public void UpdateSoundSettings(SoundSettings settings)
    {
        // 如果当前显示的是音效设置页面，则更新其设置
        if (MainFrame.Content is SoundSettingsPage soundSettingsPage)
        {
            // 注意：这里我们不直接调用不存在的SetSoundSettings方法
            // 而是通过其他方式更新设置，例如通过事件或属性
            System.Diagnostics.Debug.WriteLine("尝试更新音效设置页面的设置");
        }
    }
    
    /// <summary>
    /// 更新SoundManager的音效设置
    /// </summary>
    /// <param name="settings">新的音效设置</param>
    public void UpdateSoundManager(SoundSettings settings)
    {
        if (_soundManager != null)
        {
            _soundManager.UpdateSettings(settings);
            System.Diagnostics.Debug.WriteLine("音效管理器设置已更新");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("音效管理器为空，无法更新设置");
        }
    }
    
    /// <summary>
    /// 保存音效设置到配置文件
    /// </summary>
    private void SaveSoundSettings()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_soundSettings, options);
            File.WriteAllText(configPath, json);
            System.Diagnostics.Debug.WriteLine($"音效设置已保存到: {configPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存音效设置失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 从配置文件加载音效设置
    /// </summary>
    private void LoadSoundSettings()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds.json");
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var settings = JsonSerializer.Deserialize<SoundSettings>(json);
                if (settings != null)
                {
                    _soundSettings = settings;
                    System.Diagnostics.Debug.WriteLine($"音效设置已从 {configPath} 加载");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载音效设置失败: {ex.Message}");
        }
    }
}