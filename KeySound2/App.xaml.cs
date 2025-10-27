using System.Windows;

namespace KeySound2;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 创建并显示主窗口
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
    }
}