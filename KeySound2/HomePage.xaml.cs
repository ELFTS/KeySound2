using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace KeySound2
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            LoadCornerImages();
        }
        
        /// <summary>
        /// 加载角落图片
        /// </summary>
        private void LoadCornerImages()
        {
            try
            {
                // 尝试从资源加载左下角图片
                Uri bottomLeftUri = new Uri("pack://application:,,,/Images/bottom_left.png");
                BitmapImage bottomLeftBitmap = new BitmapImage(bottomLeftUri);
                BottomLeftImage.Source = bottomLeftBitmap;
                
                // 尝试从资源加载右下角图片
                Uri bottomRightUri = new Uri("pack://application:,,,/Images/bottom_right.png");
                BitmapImage bottomRightBitmap = new BitmapImage(bottomRightUri);
                BottomRightImage.Source = bottomRightBitmap;
            }
            catch (Exception ex)
            {
                // 静默处理图片加载错误
                System.Diagnostics.Debug.WriteLine($"加载角落图片失败: {ex.Message}");
            }
        }
    }
}