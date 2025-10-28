using System.Threading.Tasks;
using System.Windows;

namespace KeySound2;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 创建并显示启动窗口
        SplashScreenWindow splashScreen = new SplashScreenWindow();
        
        // 在后台加载主窗口
        MainWindow mainWindow = new MainWindow();
        
        // 显示启动动画并等待
        await splashScreen.ShowSplashScreen(2000); // 显示2秒
        
        // 显示主窗口
        mainWindow.Show();
    }
}