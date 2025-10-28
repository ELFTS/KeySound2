using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace KeySound2
{
    /// <summary>
    /// SplashScreenWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// 显示启动窗口并等待指定时间
        /// </summary>
        /// <param name="milliseconds">显示时间（毫秒）</param>
        public async Task ShowSplashScreen(int milliseconds)
        {
            this.Show();
            
            // 等待指定时间
            await Task.Delay(milliseconds);
            
            // 执行淡出动画
            await FadeOutAnimation();
            
            this.Close();
        }
        
        /// <summary>
        /// 执行淡出动画
        /// </summary>
        private async Task FadeOutAnimation()
        {
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            var taskCompletionSource = new TaskCompletionSource<bool>();
            
            animation.Completed += (sender, e) => taskCompletionSource.SetResult(true);
            
            this.BeginAnimation(UIElement.OpacityProperty, animation);
            
            await taskCompletionSource.Task;
        }
    }
}