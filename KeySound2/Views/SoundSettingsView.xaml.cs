using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using KeySound2.Components;
using KeySound2.Services;
using KeySound2.Models;

namespace KeySound2.Views
{
    /// <summary>
    /// SoundSettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class SoundSettingsView : UserControl
    {
        // 服务和管理器引用
        private SoundService _soundService;
        private ProfileManager _profileManager;
        private AudioFileManager _audioFileManager;
        
        // 当前选中的按键
        private System.Windows.Input.Key _selectedKey = System.Windows.Input.Key.None;
        
        // 音频文件列表
        private ObservableCollection<string> _audioFilesList = new ObservableCollection<string>();

        public SoundSettingsView()
        {
            InitializeComponent();
            // 设置ListBox的数据源
            AudioFilesListBox.ItemsSource = _audioFilesList;
        }

        // 当虚拟键盘上的按键被选中时
        private void VirtualKeyboardControl_KeySelected(object sender, VirtualKeyEventArgs e)
        {
            _selectedKey = e.Key;
            SelectedKeyText.Text = $"当前选中按键: {e.Key}";
            
            // 显示当前按键的音效路径
            UpdateSoundPathDisplay();
        }

        // 播放当前音效按钮点击事件
        private void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKey == System.Windows.Input.Key.None)
            {
                MessageBox.Show("请先选择一个按键", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 播放当前按键的音效
            _soundService?.PlaySound(_selectedKey);
        }

        // 更新音效路径显示
        private void UpdateSoundPathDisplay()
        {
            if (_selectedKey != System.Windows.Input.Key.None && _profileManager != null)
            {
                var soundPath = _profileManager.GetKeySound(_selectedKey);
                SoundPathTextBox.Text = soundPath ?? "";
            }
        }

        // 设置服务和管理器引用
        public void SetServices(SoundService soundService, ProfileManager profileManager, AudioFileManager audioFileManager)
        {
            _soundService = soundService;
            _profileManager = profileManager;
            _audioFileManager = audioFileManager;
            
            // 初始化界面
            InitializeProfileUI();
            RefreshAudioFilesList();
        }
        
        // 初始化声音方案界面
        private void InitializeProfileUI()
        {
            if (_profileManager == null) return;
            
            // 绑定方案列表
            ProfileComboBox.ItemsSource = _profileManager.Profiles;
            ProfileComboBox.DisplayMemberPath = "Name";
            ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
        }
        
        // 方案选择变化事件
        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_profileManager == null || ProfileComboBox.SelectedItem == null) return;
            
            var selectedProfile = (SoundProfile)ProfileComboBox.SelectedItem;
            _profileManager.SetCurrentProfile(selectedProfile);
            
            // 更新按键音效路径显示
            UpdateSoundPathDisplay();
            
            // 强制刷新音频文件列表
            RefreshAudioFilesList();
        }
        
        // 新建方案按钮点击事件
        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null) return;
            
            // 弹出输入框获取方案名称
            var dialog = new Window
            {
                Title = "新建方案",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = Window.GetWindow(this)
            };
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var textBox = new TextBox
            {
                Margin = new Thickness(20, 20, 20, 10),
                Text = "请输入方案名称"
            };
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 20, 20)
            };
            
            var okButton = new Button
            {
                Content = "确定",
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            
            var cancelButton = new Button
            {
                Content = "取消",
                IsCancel = true
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            Grid.SetRow(textBox, 0);
            Grid.SetRow(buttonPanel, 1);
            
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            
            dialog.Content = grid;
            
            bool confirmed = false;
            okButton.Click += (s, args) => {
                confirmed = true;
                dialog.Close();
            };
            
            cancelButton.Click += (s, args) => {
                dialog.Close();
            };
            
            dialog.ShowDialog();
            
            if (confirmed && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var profile = _profileManager.CreateProfile(textBox.Text.Trim());
                
                // 刷新方案列表
                ProfileComboBox.ItemsSource = null;
                ProfileComboBox.ItemsSource = _profileManager.Profiles;
                ProfileComboBox.SelectedItem = profile;
            }
        }
        
        // 删除方案按钮点击事件
        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || ProfileComboBox.SelectedItem == null) return;
            
            var selectedProfile = (SoundProfile)ProfileComboBox.SelectedItem;
            
            // 确认删除
            var result = MessageBox.Show($"确定要删除方案 \"{selectedProfile.Name}\" 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _profileManager.DeleteProfile(selectedProfile);
                
                // 刷新方案列表
                ProfileComboBox.ItemsSource = null;
                ProfileComboBox.ItemsSource = _profileManager.Profiles;
                ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
                
                // 刷新音频文件列表
                RefreshAudioFilesList();
            }
        }
        
        // 导出方案按钮点击事件
        private void ExportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || ProfileComboBox.SelectedItem == null) return;
            
            var selectedProfile = (SoundProfile)ProfileComboBox.SelectedItem;
            
            var saveFileDialog = new SaveFileDialog
            {
                Title = "导出声音方案",
                Filter = "ZIP文件|*.zip",
                FileName = $"{selectedProfile.Name}.zip"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var success = _profileManager.ExportProfile(selectedProfile, saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("声音方案导出成功！", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("声音方案导出失败！", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 导入方案按钮点击事件
        private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null) return;
            
            var openFileDialog = new OpenFileDialog
            {
                Title = "导入声音方案",
                Filter = "ZIP文件|*.zip"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var profile = _profileManager.ImportProfile(openFileDialog.FileName);
                    if (profile != null)
                    {
                        // 刷新方案列表
                        ProfileComboBox.ItemsSource = null;
                        ProfileComboBox.ItemsSource = _profileManager.Profiles;
                        ProfileComboBox.SelectedItem = profile;
                        
                        MessageBox.Show($"声音方案 \"{profile.Name}\" 导入成功！", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("声音方案导入失败！", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 添加音频文件按钮点击事件
        private void AddAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "添加音频文件",
                Filter = "音频文件|*.wav;*.mp3;*.aac;*.wma;*.flac|所有文件|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    // 直接导入音频文件到当前方案文件夹，保持原始文件名
                    _profileManager?.ImportSoundToCurrentProfile(fileName);
                }
                
                // 刷新音频文件列表（从当前方案文件夹加载）
                RefreshAudioFilesList();
            }
        }
        
        // 刷新音频文件列表
        private void RefreshAudioFilesList()
        {
            if (_profileManager == null || _profileManager.CurrentProfile == null) return;
            
            try
            {
                // 清空当前列表
                _audioFilesList.Clear();
                
                // 从当前方案的KeySounds文件夹加载音频文件
                var keySoundsDirectory = Path.Combine(_profileManager.CurrentProfile.FilePath, "KeySounds");
                System.Diagnostics.Debug.WriteLine($"尝试加载目录: {keySoundsDirectory}");
                
                if (Directory.Exists(keySoundsDirectory))
                {
                    var supportedExtensions = new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" };
                    var soundFiles = Directory.GetFiles(keySoundsDirectory, "*.*", SearchOption.AllDirectories)
                        .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
                    
                    // 添加文件到ObservableCollection
                    foreach (var file in soundFiles)
                    {
                        _audioFilesList.Add(file);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"找到 {_audioFilesList.Count} 个音频文件");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"目录不存在: {keySoundsDirectory}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载音频文件列表失败: {ex.Message}");
            }
        }
        
        // 刷新音频文件按钮点击事件
        private void RefreshAudioFilesButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudioFilesList();
        }
        
        // 音频文件列表双击事件
        private void AudioFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AudioFilesListBox.SelectedItem is string filePath && _selectedKey != System.Windows.Input.Key.None)
            {
                // 检查按键是否已经设置了音效
                var currentSoundPath = _profileManager?.GetKeySound(_selectedKey);
                string message;
                
                if (!string.IsNullOrEmpty(currentSoundPath) && File.Exists(currentSoundPath))
                {
                    message = $"确定要将按键 {_selectedKey} 的音效从\n{System.IO.Path.GetFileName(currentSoundPath)}\n更换为\n{System.IO.Path.GetFileName(filePath)}\n吗？";
                }
                else
                {
                    message = $"确定要将选中的音频文件设置为按键 {_selectedKey} 的音效吗？";
                }
                
                // 确认操作
                var result = MessageBox.Show(message, "确认设置", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 设置选中按键的音效
                    _profileManager?.SetKeySound(_selectedKey, filePath);
                    
                    // 更新显示
                    UpdateSoundPathDisplay();
                    
                    // 给用户提示
                    MessageBox.Show($"已将选中的音频文件设置为按键 {_selectedKey} 的音效", "设置成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_selectedKey == System.Windows.Input.Key.None)
            {
                MessageBox.Show("请先选择一个按键", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        // 重命名音频文件按钮点击事件
        private void RenameAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var filePath = button?.DataContext as string;
            
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            
            // 获取文件名（不含路径）
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            
            // 弹出输入框获取新文件名
            var dialog = new Window
            {
                Title = "重命名音频文件",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = Window.GetWindow(this)
            };
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var textBox = new TextBox
            {
                Margin = new Thickness(20, 20, 20, 10),
                Text = fileNameWithoutExtension
            };
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 20, 20)
            };
            
            var okButton = new Button
            {
                Content = "确定",
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            
            var cancelButton = new Button
            {
                Content = "取消",
                IsCancel = true
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            Grid.SetRow(textBox, 0);
            Grid.SetRow(buttonPanel, 1);
            
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            
            dialog.Content = grid;
            
            bool confirmed = false;
            okButton.Click += (s, args) => {
                confirmed = true;
                dialog.Close();
            };
            
            cancelButton.Click += (s, args) => {
                dialog.Close();
            };
            
            dialog.ShowDialog();
            
            if (confirmed && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                try
                {
                    var newFileName = textBox.Text.Trim() + extension;
                    var directory = Path.GetDirectoryName(filePath);
                    var newFilePath = Path.Combine(directory, newFileName);
                    
                    // 检查新文件名是否已存在
                    if (File.Exists(newFilePath))
                    {
                        var result = MessageBox.Show($"文件 \"{newFileName}\" 已存在，是否覆盖？", "确认覆盖", 
                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (result != MessageBoxResult.Yes)
                        {
                            return;
                        }
                        
                        // 删除已存在的文件
                        File.Delete(newFilePath);
                    }
                    
                    // 重命名文件
                    File.Move(filePath, newFilePath);
                    
                    // 刷新音频文件列表
                    RefreshAudioFilesList();
                    
                    MessageBox.Show("文件重命名成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"重命名文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 删除音频文件按钮点击事件
        private void DeleteAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var filePath = button?.DataContext as string;
            
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            
            // 确认删除
            var fileName = Path.GetFileName(filePath);
            var result = MessageBox.Show($"确定要删除音频文件 \"{fileName}\" 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 删除文件
                    File.Delete(filePath);
                    
                    // 刷新音频文件列表
                    RefreshAudioFilesList();
                    
                    MessageBox.Show("文件删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}