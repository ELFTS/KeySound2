using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KeySound2.Components
{
    /// <summary>
    /// SidebarItem.xaml 的交互逻辑
    /// </summary>
    public partial class SidebarItem : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SidebarItem), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(SidebarItem), new PropertyMetadata(default(string), OnIconChanged));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(SidebarItem), new PropertyMetadata(false, OnIsActiveChanged));

        public event EventHandler Click;

        // 定义默认颜色
        private static readonly Brush ActiveBackgroundBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
        private static readonly Brush InactiveBackgroundBrush = Brushes.Transparent;
        private static readonly Brush HoverBackgroundBrush = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
        private static readonly Brush DefaultForegroundBrush = Brushes.Black;
        private static readonly Brush ActiveForegroundBrush = Brushes.Blue; // 默认激活前景色

        public SidebarItem()
        {
            InitializeComponent();

            // 绑定属性
            TextPresenter.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Text") { Source = this });
            SetBinding(IconProperty, new System.Windows.Data.Binding("Icon") { Source = this });

            // 设置默认前景色
            TextPresenter.Foreground = DefaultForegroundBrush;

            // 设置鼠标事件
            MouseLeftButtonUp += SidebarItem_MouseLeftButtonUp;
            MouseEnter += SidebarItem_MouseEnter;
            MouseLeave += SidebarItem_MouseLeave;
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SidebarItem)d;
            var imagePath = e.NewValue as string;

            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    // 检查是否是资源路径
                    if (imagePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                    {
                        // 从嵌入资源加载图片
                        var resourceName = imagePath.Substring(6); // 移除 "res://" 前缀
                        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        // 修正资源路径格式
                        var resourcePath = $"KeySound2.Assets.Icons.{resourceName}";
                        
                        using (var stream = assembly.GetManifestResourceStream(resourcePath))
                        {
                            if (stream != null)
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = stream;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                bitmap.Freeze(); // 冻结以提高性能
                                control.IconPresenter.Source = bitmap;
                            }
                            else
                            {
                                // 如果资源未找到，尝试使用pack URI方式加载
                                var packUri = new Uri($"pack://application:,,,/KeySound2;component/Assets/Icons/{resourceName}");
                                var bitmap = new BitmapImage(packUri);
                                control.IconPresenter.Source = bitmap;
                            }
                        }
                    }
                    else if (imagePath.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                    {
                        // 从应用程序资源加载图片
                        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                        control.IconPresenter.Source = bitmap;
                    }
                    else
                    {
                        // 从文件系统加载图片
                        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                        control.IconPresenter.Source = bitmap;
                    }
                }
                catch (Exception ex)
                {
                    // 如果加载图片失败，则清空图片
                    System.Diagnostics.Debug.WriteLine($"加载图标失败: {ex.Message}");
                    control.IconPresenter.Source = null;
                }
            }
            else
            {
                // 如果没有图标路径，则清空图片
                control.IconPresenter.Source = null;
            }
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SidebarItem)d;
            var isActive = (bool)e.NewValue;

            // 更新背景色
            control.MainBorder.Background = isActive ? ActiveBackgroundBrush : InactiveBackgroundBrush;

            // 更新前景色
            control.TextPresenter.Foreground = isActive ? ActiveForegroundBrush : DefaultForegroundBrush;
        }

        private void SidebarItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        private void SidebarItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsActive)
            {
                MainBorder.Background = HoverBackgroundBrush;
            }
        }

        private void SidebarItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsActive)
            {
                MainBorder.Background = InactiveBackgroundBrush;
            }
        }
    }
}