using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace KeySound2;

/// <summary>
/// KeySoundItem.xaml 的交互逻辑
/// </summary>
public partial class KeySoundItem : UserControl
{
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.Register("Key", typeof(Key), typeof(KeySoundItem), new PropertyMetadata(Key.None));
        
    public static readonly DependencyProperty SoundFileProperty =
        DependencyProperty.Register("SoundFile", typeof(string), typeof(KeySoundItem), new PropertyMetadata(string.Empty));
        
    public static readonly DependencyProperty VolumeProperty =
        DependencyProperty.Register("Volume", typeof(int), typeof(KeySoundItem), new PropertyMetadata(50));

    public Key Key
    {
        get { return (Key)GetValue(KeyProperty); }
        set { SetValue(KeyProperty, value); }
    }
    
    public string KeyName => Key.ToString();

    public string SoundFile
    {
        get { return (string)GetValue(SoundFileProperty); }
        set { SetValue(SoundFileProperty, value); }
    }
    
    public int Volume
    {
        get { return (int)GetValue(VolumeProperty); }
        set { SetValue(VolumeProperty, value); }
    }

    public KeySoundItem()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void SelectSound_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Filter = "音频文件|*.wav;*.mp3;*.aac;*.wma;*.m4a|所有文件|*.*",
            Title = "选择音效文件"
        };

        if (dialog.ShowDialog() == true)
        {
            SoundFile = dialog.FileName;
        }
    }

    private void ClearSound_Click(object sender, RoutedEventArgs e)
    {
        SoundFile = string.Empty;
    }
}