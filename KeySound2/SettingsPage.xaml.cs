using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using MaterialDesignThemes.Wpf;

namespace KeySound2;

/// <summary>
/// SettingsPage.xaml 的交互逻辑
/// </summary>
public partial class SettingsPage : Page
{
    private AppSettings _appSettings;
    
    public SettingsPage()
    {
        InitializeComponent();
        _appSettings = AppSettings.Load();
        LoadSettings();
        
        // 绑定音量滑块值到文本显示
        if (DefaultVolumeSlider != null)
        {
            DefaultVolumeSlider.ValueChanged += (s, e) => {
                if (VolumeValueText != null)
                    VolumeValueText.Text = $"{Math.Round(e.NewValue)}%";
            };
        }
    }
    
    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            // 开机自动启动设置
            if (AutoStartCheckBox != null)
                AutoStartCheckBox.IsChecked = IsAutoStartEnabled();
            
            // 加载其他设置
            if (StartMinimizedCheckBox != null)
                StartMinimizedCheckBox.IsChecked = _appSettings.StartMinimized;
                
            if (DefaultVolumeSlider != null)
                DefaultVolumeSlider.Value = _appSettings.DefaultVolume;
                
            if (ShowTrayIconCheckBox != null)
                ShowTrayIconCheckBox.IsChecked = _appSettings.ShowTrayIcon;
                
            if (MinimizeToTrayCheckBox != null)
                MinimizeToTrayCheckBox.IsChecked = _appSettings.MinimizeToTray;
        }
        catch (Exception ex)
        {
            ShowMessageBox("错误", $"加载设置时出错: {ex.Message}", MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 检查是否启用了开机自动启动
    /// </summary>
    /// <returns></returns>
    private bool IsAutoStartEnabled()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue("KeySound2") != null;
            }
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 设置开机自动启动
    /// </summary>
    /// <param name="enable"></param>
    private void SetAutoStart(bool enable)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                {
                    key?.SetValue("KeySound2", System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "");
                }
                else
                {
                    key?.DeleteValue("KeySound2", false);
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessageBox("错误", $"设置开机自动启动时出错: {ex.Message}", MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 保存设置按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 保存开机自动启动设置
            if (AutoStartCheckBox != null)
                SetAutoStart(AutoStartCheckBox.IsChecked == true);
            
            // 保存其他设置到配置文件
            if (_appSettings != null)
            {
                _appSettings.StartMinimized = StartMinimizedCheckBox?.IsChecked ?? false;
                _appSettings.DefaultVolume = DefaultVolumeSlider?.Value ?? 50.0;
                _appSettings.ShowTrayIcon = ShowTrayIconCheckBox?.IsChecked ?? true;
                _appSettings.MinimizeToTray = MinimizeToTrayCheckBox?.IsChecked ?? true;
                
                _appSettings.Save();
            }
            
            ShowMessageBox("提示", "设置已保存！", MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowMessageBox("错误", $"保存设置时出错: {ex.Message}", MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // 重新加载设置
        LoadSettings();
    }
    
    /// <summary>
    /// 显示MaterialDesign风格的消息框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="image">消息图标类型</param>
    private async void ShowMessageBox(string title, string message, MessageBoxImage image = MessageBoxImage.Information)
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
}