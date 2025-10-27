using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Text.Json;
using MaterialDesignThemes.Wpf;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace KeySound2
{
    /// <summary>
    /// SoundSettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SoundSettingsPage : Page
    {
        private SoundSettings _soundSettings;
        private SoundManager _soundManager;
        private Key _selectedKey = Key.None;
        private bool _isSelectingKey = false;
        private List<SoundSchemeInfo> _availableSchemes = new List<SoundSchemeInfo>();
        private List<SoundFileInfo> _soundFiles = new List<SoundFileInfo>();
        private string _currentSoundsDirectory = "";
        private DialogHost _mainWindowDialogHost = null; // 主窗口的DialogHost实例
        
        public SoundSettingsPage()
        {
            InitializeComponent();
            InitializeSoundSettings();
            InitializeKeySelection();
            LoadAvailableSchemes();
            InitializeSoundFiles();
            
            // 绑定事件处理程序
            CreateSchemeButton.Click += CreateScheme_Click;
            SaveSchemeButton.Click += SaveScheme_Click;
            SelectKeyWithVirtualKeyboardButton.Click += SelectKeyWithVirtualKeyboard_Click;
            KeySelectionComboBox.SelectionChanged += KeySelectionComboBox_SelectionChanged;
            SchemeSelectionComboBox.SelectionChanged += SchemeSelectionComboBox_SelectionChanged;
            RefreshSchemesButton.Click += RefreshSchemesButton_Click;
            RenameSchemeButton.Click += RenameSchemeButton_Click;
            DeleteSchemeButton.Click += DeleteSchemeButton_Click;
            
            // 音效文件管理事件处理程序
            ImportSoundFileButton.Click += ImportSoundFileButton_Click;
            DeleteSoundFileButton.Click += DeleteSoundFileButton_Click;
            RenameSoundFileButton.Click += RenameSoundFileButton_Click;
            RefreshSoundFilesButton.Click += RefreshSoundFilesButton_Click;
            SoundFilesDataGrid.SelectionChanged += SoundFilesDataGrid_SelectionChanged;
            SetKeySoundButton.Click += SetKeySoundButton_Click; // 添加设置按键音效按钮事件处理程序
            
            // 绑定虚拟键盘事件
            VirtualKeyboardControl.OnKeyPressed += VirtualKeyboard_OnKeyPressed;
        }
        
        private void InitializeSoundSettings()
        {
            _soundSettings = new SoundSettings();
            // 设置默认音效
            string defaultSoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", "default.wav");
            if (File.Exists(defaultSoundPath))
            {
                _soundSettings.DefaultSoundPath = defaultSoundPath;
            }
            
            _soundManager = new SoundManager(_soundSettings);
            // UpdateCurrentSchemeDisplay(); // 移除对已删除控件的引用
        }
        
        private void InitializeKeySelection()
        {
            // 初始化按键选择下拉框
            var keyList = Enum.GetValues(typeof(Key))
                .Cast<Key>()
                .Where(k => k != Key.None)
                .OrderBy(k => k.ToString())
                .ToList();
            
            KeySelectionComboBox.ItemsSource = keyList;
            KeySelectionComboBox.SelectedItem = Key.A; // 默认选择A键
            _selectedKey = Key.A;
            UpdateSelectedKeyDisplay();
            UpdateCurrentSoundPathDisplay();
        }
        
        private void InitializeSoundFiles()
        {
            // 初始化音效文件列表
            UpdateCurrentSoundsDirectory();
            LoadSoundFiles();
        }
        
        private void UpdateCurrentSoundsDirectory()
        {
            // 根据当前选择的声音方案更新音效文件目录
            if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme && 
                !string.IsNullOrEmpty(selectedScheme.Path))
            {
                _currentSoundsDirectory = selectedScheme.Path;
            }
            else
            {
                _currentSoundsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
            }
        }
        
        private void LoadSoundFiles()
        {
            _soundFiles.Clear();
            
            if (Directory.Exists(_currentSoundsDirectory))
            {
                try
                {
                    var soundFiles = Directory.GetFiles(_currentSoundsDirectory, "*.*")
                        .Where(file => file.ToLower().EndsWith(".wav") || 
                                      file.ToLower().EndsWith(".mp3") || 
                                      file.ToLower().EndsWith(".aac") || 
                                      file.ToLower().EndsWith(".wma") || 
                                      file.ToLower().EndsWith(".m4a"))
                        .ToList();
                    
                    foreach (var file in soundFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        _soundFiles.Add(new SoundFileInfo
                        {
                            Name = Path.GetFileName(file),
                            FullName = file,
                            Size = GetFileSizeString(fileInfo.Length),
                            ModifiedDate = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }
                catch (Exception ex)
                {
                    ShowMessageBox("错误", $"加载音效文件列表失败: {ex.Message}", MessageBoxImage.Error);
                }
            }
            
            SoundFilesDataGrid.ItemsSource = null;
            SoundFilesDataGrid.ItemsSource = _soundFiles;
        }
        
        private string GetFileSizeString(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
        
        /// <summary>
        /// 加载可用的声音方案列表
        /// </summary>
        private void LoadAvailableSchemes()
        {
            _availableSchemes.Clear();
            
            // 添加默认方案
            _availableSchemes.Add(new SoundSchemeInfo { Name = "默认方案", Path = "" });
            
            // 查找sounds文件夹中的所有方案
            string soundsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
            if (Directory.Exists(soundsDirectory))
            {
                var schemeDirectories = Directory.GetDirectories(soundsDirectory);
                foreach (string schemeDir in schemeDirectories)
                {
                    string schemeName = Path.GetFileName(schemeDir);
                    _availableSchemes.Add(new SoundSchemeInfo { Name = schemeName, Path = schemeDir });
                }
            }
            
            SchemeSelectionComboBox.ItemsSource = _availableSchemes;
            SchemeSelectionComboBox.SelectedIndex = 0; // 默认选择第一个（默认方案）
        }
        
        /// <summary>
        /// 按键选择下拉框选择变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeySelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KeySelectionComboBox.SelectedItem is Key selectedKey)
            {
                _selectedKey = selectedKey;
                UpdateSelectedKeyDisplay();
                UpdateCurrentSoundPathDisplay();
            }
        }
        
        /// <summary>
        /// 方案选择下拉框选择变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SchemeSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme)
            {
                if (!string.IsNullOrEmpty(selectedScheme.Path))
                {
                    try
                    {
                        // 加载选中的方案
                        _soundSettings.LoadFromScheme(selectedScheme.Path);
                        _soundManager.UpdateSettings(_soundSettings);
                        // UpdateCurrentSchemeDisplay(); // 移除对已删除控件的引用
                        UpdateCurrentSoundPathDisplay();
                        ShowMessageBox("提示", $"声音方案 \"{selectedScheme.Name}\" 加载成功！");
                        
                        // 更新主窗口的音效设置
                        UpdateMainWindowSoundSettings();
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("错误", $"加载声音方案失败: {ex.Message}", MessageBoxImage.Error);
                    }
                }
            }
            
            // 更新当前音效文件目录并刷新文件列表
            UpdateCurrentSoundsDirectory();
            LoadSoundFiles();
        }
        
        /// <summary>
        /// 刷新方案按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshSchemesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAvailableSchemes();
            ShowMessageBox("提示", "方案列表已刷新！");
        }
        
        /// <summary>
        /// 更新当前选中按键显示
        /// </summary>
        private void UpdateSelectedKeyDisplay()
        {
            if (_selectedKey != Key.None)
            {
                SelectedKeyText.Text = _selectedKey.ToString();
            }
            else
            {
                SelectedKeyText.Text = "未选择";
            }
        }
        
        /// <summary>
        /// 更新当前选中按键的音效路径显示
        /// </summary>
        private void UpdateCurrentSoundPathDisplay()
        {
            if (_soundSettings != null && _selectedKey != Key.None)
            {
                string soundPath = _soundSettings.GetSoundPathForKey(_selectedKey);
                if (!string.IsNullOrEmpty(soundPath))
                {
                    CurrentSoundPathText.Text = System.IO.Path.GetFileName(soundPath);
                }
                else
                {
                    CurrentSoundPathText.Text = "未设置（使用默认音效）";
                }
            }
        }
        
        /// <summary>
        /// 更新当前声音方案显示
        /// </summary>
        private void UpdateCurrentSchemeDisplay()
        {
            // 移除此方法的实现，因为相关控件已被删除
            /*
            if (_soundSettings != null)
            {
                CurrentSchemeText.Text = _soundSettings.SoundSchemeName;
            }
            */
        }
        
        private void VirtualKeyboard_OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            // 处理虚拟键盘按键事件
            System.Diagnostics.Debug.WriteLine($"虚拟键盘按键按下: {e.Key}");
            
            // 如果正在使用虚拟键盘选择按键
            if (_isSelectingKey)
            {
                // 选择按键并退出选择模式
                _selectedKey = e.Key;
                KeySelectionComboBox.SelectedItem = e.Key;
                _isSelectingKey = false;
                UpdateSelectedKeyDisplay();
                UpdateCurrentSoundPathDisplay();
                ShowMessageBox("提示", $"已选择按键: {e.Key}");
            }
            else
            {
                // 正常播放音效
                _soundManager?.PlaySound(e.Key);
            }
        }
        
        /// <summary>
        /// 使用虚拟键盘选择按键按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectKeyWithVirtualKeyboard_Click(object sender, RoutedEventArgs e)
        {
            _isSelectingKey = true;
            ShowMessageBox("提示", "请在下方虚拟键盘中点击要设置音效的按键");
        }
        
        /// <summary>
        /// 创建声音方案按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CreateScheme_Click(object sender, RoutedEventArgs e)
        {
            // 弹出输入框让用户输入方案名称
            var result = await ShowInputDialog("创建声音方案", "请输入方案名称:");
            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    // 检查方案名称是否已存在
                    string soundsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
                    string schemeDirectory = Path.Combine(soundsDirectory, result);
                    
                    if (Directory.Exists(schemeDirectory))
                    {
                        ShowMessageBox("错误", "方案名称已存在，请使用其他名称。", MessageBoxImage.Error);
                        return;
                    }
                    
                    // 创建方案目录
                    Directory.CreateDirectory(schemeDirectory);
                    
                    // 保存当前设置到新方案
                    _soundSettings.SaveToScheme(schemeDirectory);
                    
                    // 刷新方案列表
                    LoadAvailableSchemes();
                    
                    // 选择新创建的方案
                    var newScheme = _availableSchemes.FirstOrDefault(s => s.Name == result);
                    if (newScheme != null)
                    {
                        SchemeSelectionComboBox.SelectedItem = newScheme;
                    }
                    
                    ShowMessageBox("提示", $"声音方案 \"{result}\" 创建成功！");
                }
                catch (Exception ex)
                {
                    ShowMessageBox("错误", $"创建声音方案失败: {ex.Message}", MessageBoxImage.Error);
                }
            }
        }
        
        /// <summary>
        /// 保存声音方案按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveScheme_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否选择了有效方案
            if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme && 
                !string.IsNullOrEmpty(selectedScheme.Path))
            {
                try
                {
                    // 保存方案
                    _soundSettings.SaveToScheme(selectedScheme.Path);
                    ShowMessageBox("提示", $"声音方案 \"{selectedScheme.Name}\" 保存成功！");
                    
                    // 更新主窗口的音效设置
                    UpdateMainWindowSoundSettings();
                }
                catch (Exception ex)
                {
                    ShowMessageBox("错误", $"保存声音方案失败: {ex.Message}", MessageBoxImage.Error);
                }
            }
            else
            {
                ShowMessageBox("提示", "请先选择一个有效的声音方案再保存。");
            }
        }
        
        /// <summary>
        /// 重命名方案按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RenameSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否选择了有效方案（非默认方案）
            if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme && 
                !string.IsNullOrEmpty(selectedScheme.Path))
            {
                // 弹出输入框让用户输入新方案名称
                var result = await ShowInputDialog("重命名声音方案", "请输入新的方案名称:", selectedScheme.Name);
                if (!string.IsNullOrEmpty(result) && result != selectedScheme.Name)
                {
                    try
                    {
                        // 检查新名称是否已存在
                        string soundsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
                        string newSchemeDirectory = Path.Combine(soundsDirectory, result);
                        
                        if (Directory.Exists(newSchemeDirectory))
                        {
                            ShowMessageBox("错误", "方案名称已存在，请使用其他名称。", MessageBoxImage.Error);
                            return;
                        }
                        
                        // 重命名方案目录
                        Directory.Move(selectedScheme.Path, newSchemeDirectory);
                        
                        // 刷新方案列表
                        LoadAvailableSchemes();
                        
                        // 选择重命名后的方案
                        var renamedScheme = _availableSchemes.FirstOrDefault(s => s.Name == result);
                        if (renamedScheme != null)
                        {
                            SchemeSelectionComboBox.SelectedItem = renamedScheme;
                        }
                        
                        ShowMessageBox("提示", $"声音方案已重命名为 \"{result}\"！");
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("错误", $"重命名声音方案失败: {ex.Message}", MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                ShowMessageBox("提示", "请先选择一个有效的声音方案再重命名。");
            }
        }
        
        /// <summary>
        /// 删除方案按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否选择了有效方案（非默认方案）
            if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme && 
                !string.IsNullOrEmpty(selectedScheme.Path))
            {
                // 确认删除
                var confirmResult = await ShowConfirmDialog("删除声音方案", 
                    $"确定要删除声音方案 \"{selectedScheme.Name}\" 吗？\n此操作不可撤销。");
                
                if (confirmResult == true)
                {
                    try
                    {
                        // 删除方案目录
                        Directory.Delete(selectedScheme.Path, true);
                        
                        // 刷新方案列表
                        LoadAvailableSchemes();
                        
                        // 选择默认方案
                        SchemeSelectionComboBox.SelectedIndex = 0;
                        
                        ShowMessageBox("提示", $"声音方案 \"{selectedScheme.Name}\" 已删除！");
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("错误", $"删除声音方案失败: {ex.Message}", MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                ShowMessageBox("提示", "请先选择一个有效的声音方案再删除。");
            }
        }
        
        /// <summary>
        /// 导入音效文件按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportSoundFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "音频文件|*.wav;*.mp3;*.aac;*.wma;*.m4a|所有文件|*.*",
                Title = "选择音效文件",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 确保当前音效目录存在
                    if (!Directory.Exists(_currentSoundsDirectory))
                    {
                        Directory.CreateDirectory(_currentSoundsDirectory);
                    }
                    
                    int importedCount = 0;
                    foreach (string filePath in dialog.FileNames)
                    {
                        string fileName = Path.GetFileName(filePath);
                        string destinationPath = Path.Combine(_currentSoundsDirectory, fileName);
                        
                        // 如果文件已存在，添加数字后缀
                        if (File.Exists(destinationPath))
                        {
                            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                            string extension = Path.GetExtension(fileName);
                            int counter = 1;
                            
                            string newFileName = $"{nameWithoutExtension}({counter}){extension}";
                            destinationPath = Path.Combine(_currentSoundsDirectory, newFileName);
                            
                            while (File.Exists(destinationPath))
                            {
                                counter++;
                                newFileName = $"{nameWithoutExtension}({counter}){extension}";
                                destinationPath = Path.Combine(_currentSoundsDirectory, newFileName);
                            }
                        }
                        
                        File.Copy(filePath, destinationPath);
                        importedCount++;
                    }
                    
                    LoadSoundFiles();
                    ShowMessageBox("提示", $"成功导入 {importedCount} 个音效文件！");
                }
                catch (Exception ex)
                {
                    ShowMessageBox("错误", $"导入音效文件失败: {ex.Message}", MessageBoxImage.Error);
                }
            }
        }
        
        /// <summary>
        /// 删除音效文件按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteSoundFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (SoundFilesDataGrid.SelectedItem is SoundFileInfo selectedFile)
            {
                // 确认删除
                var confirmResult = await ShowConfirmDialog("删除音效文件", 
                    $"确定要删除音效文件 \"{selectedFile.Name}\" 吗？\n此操作不可撤销。");
                
                if (confirmResult == true)
                {
                    try
                    {
                        if (File.Exists(selectedFile.FullName))
                        {
                            File.Delete(selectedFile.FullName);
                            LoadSoundFiles();
                            ShowMessageBox("提示", $"音效文件 \"{selectedFile.Name}\" 已删除！");
                        }
                        else
                        {
                            ShowMessageBox("错误", "文件不存在！", MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("错误", $"删除音效文件失败: {ex.Message}", MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                ShowMessageBox("提示", "请先选择一个音效文件再删除。");
            }
        }
        
        /// <summary>
        /// 重命名音效文件按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RenameSoundFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (SoundFilesDataGrid.SelectedItem is SoundFileInfo selectedFile)
            {
                string currentName = Path.GetFileNameWithoutExtension(selectedFile.Name);
                string extension = Path.GetExtension(selectedFile.Name);
                
                // 弹出输入框让用户输入新文件名
                var result = await ShowInputDialog("重命名音效文件", "请输入新的文件名:", currentName);
                if (!string.IsNullOrEmpty(result) && result != currentName)
                {
                    try
                    {
                        string newFileName = result + extension;
                        string newFilePath = Path.Combine(_currentSoundsDirectory, newFileName);
                        
                        // 检查新名称是否已存在
                        if (File.Exists(newFilePath))
                        {
                            ShowMessageBox("错误", "文件名已存在，请使用其他名称。", MessageBoxImage.Error);
                            return;
                        }
                        
                        // 重命名文件
                        File.Move(selectedFile.FullName, newFilePath);
                        
                        LoadSoundFiles();
                        ShowMessageBox("提示", $"音效文件已重命名为 \"{newFileName}\"！");
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("错误", $"重命名音效文件失败: {ex.Message}", MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                ShowMessageBox("提示", "请先选择一个音效文件再重命名。");
            }
        }
        
        /// <summary>
        /// 刷新音效文件按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshSoundFilesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSoundFiles();
            ShowMessageBox("提示", "音效文件列表已刷新！");
        }
        
        /// <summary>
        /// 设置按键音效按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetKeySoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKey == Key.None)
            {
                ShowMessageBox("提示", "请选择一个按键");
                return;
            }
            
            if (SoundFilesDataGrid.SelectedItem is SoundFileInfo selectedFile)
            {
                try
                {
                    // 设置按键音效
                    _soundSettings.SetSoundForKey(_selectedKey, selectedFile.FullName);
                    UpdateCurrentSoundPathDisplay();
                    ShowMessageBox("提示", $"{_selectedKey} 键的音效设置成功！");
                    
                    // 如果当前选择了有效的方案，则保存到该方案
                    if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme && 
                        !string.IsNullOrEmpty(selectedScheme.Path))
                    {
                        _soundSettings.SaveToScheme(selectedScheme.Path);
                        ShowMessageBox("提示", $"声音方案 \"{selectedScheme.Name}\" 已更新！");
                    }
                    
                    // 更新主窗口的音效设置
                    UpdateMainWindowSoundSettings();
                }
                catch (Exception ex)
                {
                    ShowMessageBox("错误", $"设置 {_selectedKey} 键音效失败: {ex.Message}", MessageBoxImage.Error);
                }
            }
            else
            {
                ShowMessageBox("提示", "请先选择一个音效文件。");
            }
        }
        
        /// <summary>
        /// 音效文件列表选择变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundFilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundFilesDataGrid.SelectedItem is SoundFileInfo selectedFile)
            {
                // 可以在这里添加选中文件后的操作，比如预览等
            }
        }
        
        /// <summary>
        /// 更新主窗口的音效设置
        /// </summary>
        private void UpdateMainWindowSoundSettings()
        {
            // 获取主窗口实例并更新其音效设置
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.UpdateSoundManager(_soundSettings);
            }
        }
        
        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">提示信息</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入的值</returns>
        private async System.Threading.Tasks.Task<string> ShowInputDialog(string title, string message, string defaultValue = "")
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
                Margin = new Thickness(0, 0, 0, 8)
            });

            // 添加输入框
            var textBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 16)
            };
            dialogContent.Children.Add(textBox);

            // 添加按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            // 添加"确定"按钮
            var okButton = new System.Windows.Controls.Button
            {
                Content = "确定",
                Style = (Style)FindResource("MaterialDesignFlatButton"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            okButton.CommandParameter = true;
            okButton.Command = DialogHost.CloseDialogCommand;
            buttonPanel.Children.Add(okButton);

            // 添加"取消"按钮
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "取消",
                Style = (Style)FindResource("MaterialDesignFlatButton")
            };
            cancelButton.CommandParameter = false;
            cancelButton.Command = DialogHost.CloseDialogCommand;
            buttonPanel.Children.Add(cancelButton);

            dialogContent.Children.Add(buttonPanel);

            // 显示对话框并返回结果
            var result = await DialogHost.Show(dialogContent, "RootDialog");
            
            // 如果用户点击确定，返回输入的值
            if (result is bool boolResult && boolResult)
            {
                return textBox.Text;
            }
            
            return null;
        }
        
        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <returns>用户选择结果：true=是，false=否</returns>
        private async System.Threading.Tasks.Task<bool?> ShowConfirmDialog(string title, string message)
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

            // 添加按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            // 添加"是"按钮
            var yesButton = new System.Windows.Controls.Button
            {
                Content = "是",
                Style = (Style)FindResource("MaterialDesignFlatButton"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            yesButton.CommandParameter = true;
            yesButton.Command = DialogHost.CloseDialogCommand;
            buttonPanel.Children.Add(yesButton);

            // 添加"否"按钮
            var noButton = new System.Windows.Controls.Button
            {
                Content = "否",
                Style = (Style)FindResource("MaterialDesignFlatButton")
            };
            noButton.CommandParameter = false;
            noButton.Command = DialogHost.CloseDialogCommand;
            buttonPanel.Children.Add(noButton);

            dialogContent.Children.Add(buttonPanel);

            // 显示对话框并返回结果
            var result = await DialogHost.Show(dialogContent, "RootDialog");
            return result as bool?;
        }
        
        /// <summary>
        /// 显示MaterialDesign风格的消息框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="image">消息图标类型</param>
        private async void ShowMessageBox(string title, string message, MessageBoxImage image = MessageBoxImage.Information)
        {
            // 获取主窗口实例和DialogHost
            if (Application.Current.MainWindow is MainWindow mainWindow && _mainWindowDialogHost == null)
            {
                _mainWindowDialogHost = mainWindow.FindName("MainDialogHost") as DialogHost;
            }

            if (_mainWindowDialogHost == null)
            {
                // 如果找不到DialogHost，直接返回
                return;
            }

            // 检查是否已经有对话框打开
            if (_mainWindowDialogHost.IsOpen)
            {
                return;
            }

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
            await _mainWindowDialogHost.Show(dialogContent);
        }
    }
    
    /// <summary>
    /// 声音方案信息类
    /// </summary>
    public class SoundSchemeInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
    
    /// <summary>
    /// 音效文件信息类
    /// </summary>
    public class SoundFileInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Size { get; set; }
        public string ModifiedDate { get; set; }
    }
}