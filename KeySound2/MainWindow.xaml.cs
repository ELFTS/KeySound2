using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using KeySound2.Views;
using KeySound2.Components;
using KeySound2.Services;
using KeySound2.Models;
using MaterialDesignThemes.Wpf;

namespace KeySound2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<SidebarItem> sidebarItems = new List<SidebarItem>();
        
        // 服务和管理器
        private readonly SoundService _soundService;
        private readonly ProfileManager _profileManager;
        private readonly AudioFileManager _audioFileManager;

        // 用于存储窗口正常状态时的位置和大小
        private Rect _normalWindowState = Rect.Empty;
        
        // Windows API 常量
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化服务和管理器
            _profileManager = new ProfileManager();
            _audioFileManager = new AudioFileManager();
            _soundService = new SoundService(_profileManager);
            
            // 初始化导航
            InitializeNavigation();
            
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSidebar();
            // 默认加载主页内容
            LoadContent("Home");
            
            // 记录窗口正常状态时的位置和大小
            UpdateNormalWindowState();
        }

        // 初始化导航
        private void InitializeNavigation()
        {
            // 设置默认视图
            MainContentArea.Content = new HomeView();
            
            // 绑定侧边栏项目事件
            // 注意：这里我们不直接绑定控件事件，因为控件是在InitializeSidebar中动态创建的
        }

        private void InitializeSidebar()
        {
            // 清空现有项目
            SidebarPanel.Children.Clear();
            sidebarItems.Clear();

            // 添加侧边栏菜单项
            var homeItem = new SidebarItem
            {
                Text = "主页",
                Icon = "res://home.png"
            };
            homeItem.Click += (sender, e) => LoadContent("Home");
            SidebarPanel.Children.Add(homeItem);
            sidebarItems.Add(homeItem);

            var soundItem = new SidebarItem
            {
                Text = "音效设置",
                Icon = "res://music.png"
            };
            soundItem.Click += (sender, e) => LoadContent("Sound");
            SidebarPanel.Children.Add(soundItem);
            sidebarItems.Add(soundItem);

            var settingsItem = new SidebarItem
            {
                Text = "系统设置",
                Icon = "res://settings.png"
            };
            settingsItem.Click += (sender, e) => LoadContent("Settings");
            SidebarPanel.Children.Add(settingsItem);
            sidebarItems.Add(settingsItem);

            var aboutItem = new SidebarItem
            {
                Text = "关于",
                Icon = "res://info.png"
            };
            aboutItem.Click += (sender, e) => LoadContent("About");
            SidebarPanel.Children.Add(aboutItem);
            sidebarItems.Add(aboutItem);

            // 设置默认选中项
            if (sidebarItems.Count > 0)
            {
                sidebarItems[0].IsActive = true;
            }
        }

        private void LoadContent(string contentType)
        {
            try
            {
                // 先取消所有侧边栏项目的选中状态
                foreach (var item in sidebarItems)
                {
                    item.IsActive = false;
                }

                UserControl content = contentType switch
                {
                    "Home" => new HomeView(),
                    "Sound" => new SoundSettingsView(),
                    "Settings" => new SettingsView(),
                    "About" => new AboutView(),
                    _ => new HomeView()
                };

                // 如果是音效设置视图，设置服务引用
                if (content is SoundSettingsView soundSettingsView)
                {
                    soundSettingsView.SetServices(_soundService, _profileManager, _audioFileManager);
                }

                MainContentArea.Content = content;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载内容时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 窗口控制事件

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 记录鼠标点击时的位置
            var position = e.GetPosition(this);
            
            // 判断是单击还是双击
            if (e.ClickCount == 2 && position.Y <= 30) // 30是标题栏高度
            {
                // 双击标题栏最大化/还原窗口
                ToggleWindowState();
            }
            else if (position.Y <= 30) // 点击在标题栏区域
            {
                // 拖动窗口
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleWindowState()
        {
            if (WindowState == WindowState.Maximized)
            {
                // 还原窗口
                WindowState = WindowState.Normal;
                MaximizeIcon.Kind = PackIconKind.WindowMaximize;
            }
            else
            {
                // 记录当前窗口状态
                UpdateNormalWindowState();
                
                // 最大化窗口
                WindowState = WindowState.Maximized;
                MaximizeIcon.Kind = PackIconKind.WindowRestore;
            }
        }
        
        /// <summary>
        /// 更新窗口正常状态时的位置和大小
        /// </summary>
        private void UpdateNormalWindowState()
        {
            if (WindowState == WindowState.Normal)
            {
                _normalWindowState = new Rect(Left, Top, Width, Height);
            }
        }

        #endregion

        #region 窗口消息处理

        // 重写此方法以处理窗口消息
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 获取窗口句柄
            var handle = new WindowInteropHelper(this).Handle;
            
            // 添加窗口消息钩子
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        // 窗口消息处理函数
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_GETMINMAXINFO:
                    // 当窗口最大化时，调整其大小以避免遮挡任务栏
                    if (WindowState == WindowState.Normal)
                    {
                        UpdateNormalWindowState();
                    }
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        // 处理WM_GETMINMAXINFO消息
        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            // 获取屏幕工作区（不包括任务栏）
            var monitor = NativeMethods.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new NativeMethods.MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                
                if (NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var minMaxInfo = Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
                    
                    // 设置最大化时的窗口位置和大小，避免遮挡任务栏
                    minMaxInfo.ptMaxPosition.X = Math.Abs(monitorInfo.rcWork.Left - monitorInfo.rcMonitor.Left);
                    minMaxInfo.ptMaxPosition.Y = Math.Abs(monitorInfo.rcWork.Top - monitorInfo.rcMonitor.Top);
                    minMaxInfo.ptMaxSize.X = Math.Abs(monitorInfo.rcWork.Right - monitorInfo.rcWork.Left);
                    minMaxInfo.ptMaxSize.Y = Math.Abs(monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top);
                    
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                }
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 释放资源
            _soundService?.Dispose();
        }
    }
    
    // Windows API 调用所需的结构和方法
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }
    }
}