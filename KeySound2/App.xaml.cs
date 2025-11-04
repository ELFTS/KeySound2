using System;
using System.IO;
using System.Windows;

namespace KeySound2
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 应用程序启动逻辑（如果需要）可以在这里添加
            // 注意：不要在这里创建MainWindow实例，因为App.xaml中已经设置了StartupUri
        }
    }
}