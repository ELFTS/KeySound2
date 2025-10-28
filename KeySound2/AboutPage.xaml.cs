using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Media.Imaging;

namespace KeySound2;

/// <summary>
/// AboutPage.xaml 的交互逻辑
/// </summary>
public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        LoadLogoImage();
    }
    
    /// <summary>
    /// 加载Logo图片
    /// </summary>
    private void LoadLogoImage()
    {
        try
        {
            // 尝试从资源加载Logo图片
            Uri logoUri = new Uri("pack://application:,,,/Images/logo.png");
            BitmapImage logoBitmap = new BitmapImage(logoUri);
            LogoImage.Source = logoBitmap;
        }
        catch (Exception ex)
        {
            // 静默处理图片加载错误
            System.Diagnostics.Debug.WriteLine($"加载Logo图片失败: {ex.Message}");
        }
    }
    
    
    /// <summary>
    /// 显示MaterialDesign风格的消息框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="image">消息图标类型</param>
    private async System.Threading.Tasks.Task ShowMessageBox(string title, string message, MessageBoxImage image = MessageBoxImage.Information)
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