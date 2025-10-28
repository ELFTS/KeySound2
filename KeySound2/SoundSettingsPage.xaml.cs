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
using System.Windows.Media; // 添加此引用以支持颜色相关操作

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
        private List<SoundSchemeInfo> _availableSchemes = new List<SoundSchemeInfo>();
        private List<SoundFileInfo> _soundFiles = new List<SoundFileInfo>();
        private string _currentSoundsDirectory = "";
        private bool _isDialogOpen = false; // 添加标志以防止重复打开对话框
        
        public SoundSettingsPage()
        {
            InitializeComponent();
            
            // 初始化音效设置和管理器
            _soundSettings = new SoundSettings();
            _soundManager = new SoundManager(_soundSettings);
            
            // 绑定事件处理程序
            CreateSchemeButton.Click += CreateScheme_Click;
            SaveSchemeButton.Click += SaveScheme_Click;
            SchemeSelectionComboBox.SelectionChanged += SchemeSelectionComboBox_SelectionChanged;
            RefreshSchemesButton.Click += RefreshSchemesButton_Click;
            RenameSchemeButton.Click += RenameSchemeButton_Click;
            DeleteSchemeButton.Click += DeleteSchemeButton_Click;
            
            // 音效文件管理事件处理程序
            ImportSoundFileButton.Click += ImportSoundFileButton_Click;
            DeleteSoundFileButton.Click += DeleteSoundFileButton_Click;
            RefreshSoundFilesButton.Click += RefreshSoundFilesButton_Click;
            RenameSoundFileButton.Click += RenameSoundFileButton_Click;
            SoundFilesDataGrid.SelectionChanged += SoundFilesDataGrid_SelectionChanged;
            SoundFilesDataGrid.MouseDoubleClick += SoundFilesDataGrid_MouseDoubleClick;
            SetSoundFileButton.Click += SetSoundFileButton_Click;
            ClearSoundFileButton.Click += ClearSoundFileButton_Click;
            
            // 绑定虚拟键盘事件
            VirtualKeyboardControl.OnKeyPressed += VirtualKeyboard_OnKeyPressed;
            
            // 初始化声音方案和音效文件列表
            LoadAvailableSchemes();
            InitializeSoundFiles();
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
        /// 使用虚拟键盘选择按键开关启用事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectKeyWithVirtualKeyboardToggle_Checked(object sender, RoutedEventArgs e)
        {
            ShowMessageBox("提示", "请在下方虚拟键盘中点击要设置音效的按键");
        }
        
        /// <summary>
        /// 使用虚拟键盘选择按键开关禁用事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectKeyWithVirtualKeyboardToggle_Unchecked(object sender, RoutedEventArgs e)
        {
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
                    CurrentSoundPathText.Text = soundPath;
                }
                else
                {
                    CurrentSoundPathText.Text = "未设置（使用默认音效）";
                }
            }
            else
            {
                CurrentSoundPathText.Text = "未设置（使用默认音效）";
            }
        }
        
        /// <summary>
        /// 虚拟键盘按键事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VirtualKeyboard_OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            // 处理虚拟键盘按键事件
            System.Diagnostics.Debug.WriteLine($"虚拟键盘按键按下: {e.Key}");
            
            // 直接选择按键
            _selectedKey = e.Key;
            SelectedKeyText.Text = _selectedKey.ToString();
            UpdateCurrentSoundPathDisplay();
            
            // 播放选中按键的音效
            _soundManager?.PlaySound(e.Key);
        }
        
        /// <summary>
        /// 使用虚拟键盘选择按键按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectKeyWithVirtualKeyboard_Click(object sender, RoutedEventArgs e)
        {
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
        /// 设置音效文件按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetSoundFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKey == Key.None)
            {
                ShowMessageBox("提示", "请先在虚拟键盘中选择一个按键。");
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
        /// 清除音效文件按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearSoundFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKey == Key.None)
            {
                ShowMessageBox("提示", "请先在虚拟键盘中选择一个按键。");
                return;
            }
            
            try
            {
                // 清除按键音效设置
                _soundSettings.ClearSoundForKey(_selectedKey);
                UpdateCurrentSoundPathDisplay();
                ShowMessageBox("提示", $"{_selectedKey} 键的音效已清除！");
                
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
                ShowMessageBox("错误", $"清除 {_selectedKey} 键音效失败: {ex.Message}", MessageBoxImage.Error);
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
                // 如果已选择按键，则自动设置为该按键的音效
                if (_selectedKey != Key.None)
                {
                    try
                    {
                        // 设置按键音效
                        _soundSettings.SetSoundForKey(_selectedKey, selectedFile.FullName);
                        UpdateCurrentSoundPathDisplay();
                        
                        // 如果当前选择了有效的方案，则保存到该方案
                        if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme && 
                            !string.IsNullOrEmpty(selectedScheme.Path))
                        {
                            _soundSettings.SaveToScheme(selectedScheme.Path);
                        }
                        
                        // 更新主窗口的音效设置
                        UpdateMainWindowSoundSettings();
                        
                        // 播放音效预览
                        _soundManager?.PlaySound(_selectedKey);
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("错误", $"设置 {_selectedKey} 键音效失败: {ex.Message}", MessageBoxImage.Error);
                    }
                }
            }
        }
        
        /// <summary>
        /// 音效文件列表鼠标双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundFilesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SoundFilesDataGrid.SelectedItem is SoundFileInfo selectedFile)
            {
                // 如果已选择按键，则设置为该按键的音效
                if (_selectedKey != Key.None)
                {
                    try
                    {
                        // 设置按键音效
                        _soundSettings.SetSoundForKey(_selectedKey, selectedFile.FullName);
                        UpdateCurrentSoundPathDisplay();
                        
                        // 如果当前选择了有效的方案，则保存到该方案
                        if (SchemeSelectionComboBox.SelectedItem is SoundSchemeInfo selectedScheme && 
                            !string.IsNullOrEmpty(selectedScheme.Path))
                        {
                            _soundSettings.SaveToScheme(selectedScheme.Path);
                        }
                        
                        // 更新主窗口的音效设置
                        UpdateMainWindowSoundSettings();
                        
                        // 播放音效预览
                        _soundManager?.PlaySound(_selectedKey);
                        
                        ShowMessageBox("提示", $"{_selectedKey} 键的音效已设置为: {selectedFile.Name}");
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("错误", $"设置 {_selectedKey} 键音效失败: {ex.Message}", MessageBoxImage.Error);
                    }
                }
                else
                {
                    ShowMessageBox("提示", "请先在虚拟键盘中选择一个按键。");
                }
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
            // 检查是否已有对话框打开
            if (_isDialogOpen)
                return;
                
            _isDialogOpen = true;
            
            try
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
                await DialogHost.Show(dialogContent, "RootDialog");
            }
            finally
            {
                _isDialogOpen = false;
            }
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