using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using KeySound2.Models;

namespace KeySound2.Services
{
    /// <summary>
    /// 声音方案管理器
    /// </summary>
    public class ProfileManager
    {
        private readonly string _profilesDirectory;
        private readonly List<SoundProfile> _profiles;
        private SoundProfile _currentProfile;
        
        public ProfileManager()
        {
            _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
            _profiles = new List<SoundProfile>();
            
            // 确保Profiles目录存在
            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
            }
            
            // 加载现有方案
            LoadProfiles();
            
            // 如果没有方案，则创建默认方案
            if (!_profiles.Any())
            {
                CreateDefaultProfile();
            }
            
            // 设置当前方案为第一个方案
            _currentProfile = _profiles.FirstOrDefault();
        }
        
        /// <summary>
        /// 获取所有声音方案
        /// </summary>
        public IReadOnlyList<SoundProfile> Profiles => _profiles.AsReadOnly();
        
        /// <summary>
        /// 获取当前声音方案
        /// </summary>
        public SoundProfile CurrentProfile => _currentProfile;
        
        /// <summary>
        /// 加载所有声音方案
        /// </summary>
        private void LoadProfiles()
        {
            try
            {
                var profileDirectories = Directory.GetDirectories(_profilesDirectory);
                foreach (var directory in profileDirectories)
                {
                    try
                    {
                        var profileName = Path.GetFileName(directory);
                        var configFile = Path.Combine(directory, "profile.json");
                        
                        SoundProfile profile;
                        if (File.Exists(configFile))
                        {
                            // 加载现有配置文件
                            var json = File.ReadAllText(configFile);
                            profile = JsonSerializer.Deserialize<SoundProfile>(json);
                            if (profile != null)
                            {
                                profile.FilePath = directory;
                            }
                            else
                            {
                                // 如果配置文件损坏，创建新的配置
                                profile = new SoundProfile(profileName)
                                {
                                    FilePath = directory
                                };
                            }
                        }
                        else
                        {
                            // 创建新的配置
                            profile = new SoundProfile(profileName)
                            {
                                FilePath = directory
                            };
                        }
                        
                        // 加载按键音效映射
                        LoadKeySounds(profile);
                        
                        _profiles.Add(profile);
                    }
                    catch (Exception ex)
                    {
                        // 忽略单个方案加载错误
                        System.Diagnostics.Debug.WriteLine($"加载方案失败: {directory}, 错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载声音方案时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载方案中的按键音效映射
        /// </summary>
        /// <param name="profile">声音方案</param>
        private void LoadKeySounds(SoundProfile profile)
        {
            try
            {
                var keySoundsDirectory = Path.Combine(profile.FilePath, "KeySounds");
                if (Directory.Exists(keySoundsDirectory))
                {
                    var soundFiles = Directory.GetFiles(keySoundsDirectory, "*.*", SearchOption.AllDirectories)
                        .Where(file => new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" }
                            .Contains(Path.GetExtension(file).ToLowerInvariant()));
                    
                    foreach (var soundFile in soundFiles)
                    {
                        var keyName = Path.GetFileNameWithoutExtension(soundFile);
                        if (Enum.TryParse<Key>(keyName, out var key))
                        {
                            profile.KeySounds[key] = soundFile;
                        }
                    }
                }
                
                // 加载默认音效
                var defaultSoundFile = Path.Combine(profile.FilePath, "DefaultSound.wav");
                if (File.Exists(defaultSoundFile))
                {
                    profile.DefaultSound = defaultSoundFile;
                }
                else
                {
                    // 尝试其他音频格式
                    var supportedExtensions = new[] { ".mp3", ".aac", ".wma", ".flac" };
                    foreach (var extension in supportedExtensions)
                    {
                        var file = Path.Combine(profile.FilePath, "DefaultSound" + extension);
                        if (File.Exists(file))
                        {
                            profile.DefaultSound = file;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载按键音效映射时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建默认声音方案
        /// </summary>
        private void CreateDefaultProfile()
        {
            var defaultProfile = new SoundProfile("默认方案");
            var profileDirectory = Path.Combine(_profilesDirectory, defaultProfile.Name);
            
            // 确保方案目录存在
            if (!Directory.Exists(profileDirectory))
            {
                Directory.CreateDirectory(profileDirectory);
            }
            
            defaultProfile.FilePath = profileDirectory;
            _profiles.Add(defaultProfile);
            SaveProfile(defaultProfile);
        }
        
        /// <summary>
        /// 创建新的声音方案
        /// </summary>
        /// <param name="name">方案名称</param>
        /// <returns>新创建的方案</returns>
        public SoundProfile CreateProfile(string name)
        {
            // 处理非法文件名字符
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }
            
            var profile = new SoundProfile(name);
            var profileDirectory = Path.Combine(_profilesDirectory, name);
            
            // 确保方案目录存在
            if (!Directory.Exists(profileDirectory))
            {
                Directory.CreateDirectory(profileDirectory);
            }
            
            profile.FilePath = profileDirectory;
            
            // 复制当前方案的按键映射
            if (_currentProfile != null)
            {
                foreach (var kvp in _currentProfile.KeySounds)
                {
                    profile.KeySounds[kvp.Key] = kvp.Value;
                }
                profile.DefaultSound = _currentProfile.DefaultSound;
            }
            
            _profiles.Add(profile);
            SaveProfile(profile);
            return profile;
        }
        
        /// <summary>
        /// 删除声音方案
        /// </summary>
        /// <param name="profile">要删除的方案</param>
        public void DeleteProfile(SoundProfile profile)
        {
            if (_profiles.Count <= 1)
            {
                // 至少保留一个方案
                return;
            }
            
            _profiles.Remove(profile);
            
            // 删除方案文件夹
            if (Directory.Exists(profile.FilePath))
            {
                try
                {
                    // 尝试多次删除，以防文件被锁定
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            Directory.Delete(profile.FilePath, true);
                            break; // 成功删除则退出循环
                        }
                        catch (IOException)
                        {
                            // 如果是最后一次尝试，则重新抛出异常
                            if (i == 2)
                                throw;
                            
                            // 等待一段时间后重试
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"删除方案文件夹失败: {ex.Message}");
                }
            }
            
            // 如果删除的是当前方案，则设置新的当前方案
            if (_currentProfile == profile)
            {
                _currentProfile = _profiles.FirstOrDefault();
            }
        }
        
        /// <summary>
        /// 保存声音方案
        /// </summary>
        /// <param name="profile">要保存的方案</param>
        public void SaveProfile(SoundProfile profile)
        {
            try
            {
                // 保存配置文件
                var configFile = Path.Combine(profile.FilePath, "profile.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(profile, options);
                File.WriteAllText(configFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存方案失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置当前声音方案
        /// </summary>
        /// <param name="profile">要设置为当前的方案</param>
        public void SetCurrentProfile(SoundProfile profile)
        {
            if (_profiles.Contains(profile))
            {
                _currentProfile = profile;
            }
        }
        
        /// <summary>
        /// 设置按键的音效文件路径
        /// </summary>
        /// <param name="key">按键</param>
        /// <param name="soundPath">音效文件路径</param>
        public void SetKeySound(System.Windows.Input.Key key, string soundPath)
        {
            if (_currentProfile != null && File.Exists(soundPath))
            {
                try
                {
                    var keySoundsDirectory = Path.Combine(_currentProfile.FilePath, "KeySounds");
                    if (!Directory.Exists(keySoundsDirectory))
                    {
                        Directory.CreateDirectory(keySoundsDirectory);
                    }
                    
                    var extension = Path.GetExtension(soundPath);
                    var destFileName = $"{key}{extension}";
                    var destFilePath = Path.Combine(keySoundsDirectory, destFileName);
                    
                    // 如果目标文件已存在，先删除它
                    if (File.Exists(destFilePath))
                    {
                        try 
                        {
                            File.Delete(destFilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"删除已存在的音效文件失败: {ex.Message}");
                        }
                    }
                    
                    // 复制文件
                    File.Copy(soundPath, destFilePath, true);
                    
                    // 直接引用原始文件路径
                    _currentProfile.KeySounds[key] = soundPath;
                    SaveProfile(_currentProfile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"设置按键音效失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 直接导入音效文件到当前声音方案的文件夹中，保持原始文件名
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <returns>导入后的文件路径</returns>
        public string ImportSoundToCurrentProfile(string sourceFilePath)
        {
            if (_currentProfile == null || !File.Exists(sourceFilePath))
                return null;
            
            try
            {
                var keySoundsDirectory = Path.Combine(_currentProfile.FilePath, "KeySounds");
                if (!Directory.Exists(keySoundsDirectory))
                {
                    Directory.CreateDirectory(keySoundsDirectory);
                }
                
                var fileName = Path.GetFileName(sourceFilePath);
                var destFilePath = Path.Combine(keySoundsDirectory, fileName);
                
                // 如果目标文件已存在，先删除它
                if (File.Exists(destFilePath))
                {
                    try 
                    {
                        File.Delete(destFilePath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"删除已存在的音效文件失败: {ex.Message}");
                    }
                }
                
                // 复制文件
                File.Copy(sourceFilePath, destFilePath, true);
                return destFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入音效文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 导出声音方案为ZIP文件
        /// </summary>
        /// <param name="profile">要导出的声音方案</param>
        /// <param name="exportPath">导出路径</param>
        /// <returns>是否导出成功</returns>
        public bool ExportProfile(SoundProfile profile, string exportPath)
        {
            try
            {
                // 创建临时目录用于打包
                var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);
                
                try
                {
                    // 复制profile.json文件
                    var profileJsonPath = Path.Combine(profile.FilePath, "profile.json");
                    if (File.Exists(profileJsonPath))
                    {
                        File.Copy(profileJsonPath, Path.Combine(tempDirectory, "profile.json"));
                    }
                    
                    // 复制KeySounds文件夹
                    var sourceKeySoundsDir = Path.Combine(profile.FilePath, "KeySounds");
                    var destKeySoundsDir = Path.Combine(tempDirectory, "KeySounds");
                    if (Directory.Exists(sourceKeySoundsDir))
                    {
                        CopyDirectory(sourceKeySoundsDir, destKeySoundsDir);
                    }
                    
                    // 复制默认音效文件
                    if (!string.IsNullOrEmpty(profile.DefaultSound) && File.Exists(profile.DefaultSound))
                    {
                        var defaultSoundFileName = Path.GetFileName(profile.DefaultSound);
                        File.Copy(profile.DefaultSound, Path.Combine(tempDirectory, defaultSoundFileName));
                    }
                    
                    // 创建ZIP文件
                    ZipFile.CreateFromDirectory(tempDirectory, exportPath);
                    
                    return true;
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出声音方案失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从ZIP文件导入声音方案
        /// </summary>
        /// <param name="importPath">ZIP文件路径</param>
        /// <returns>导入的声音方案，如果失败则返回null</returns>
        public SoundProfile ImportProfile(string importPath)
        {
            try
            {
                // 检查ZIP文件是否存在
                if (!File.Exists(importPath))
                {
                    System.Diagnostics.Debug.WriteLine("指定的ZIP文件不存在");
                    return null;
                }
                
                // 创建临时目录用于解压
                var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);
                
                try
                {
                    // 解压ZIP文件
                    ZipFile.ExtractToDirectory(importPath, tempDirectory);
                    
                    // 读取profile.json文件
                    var profileJsonPath = Path.Combine(tempDirectory, "profile.json");
                    if (!File.Exists(profileJsonPath))
                    {
                        System.Diagnostics.Debug.WriteLine("ZIP文件中缺少profile.json文件");
                        return null;
                    }
                    
                    var json = File.ReadAllText(profileJsonPath);
                    var profile = JsonSerializer.Deserialize<SoundProfile>(json);
                    
                    // 检查方案名称是否已存在
                    var profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                    if (Directory.Exists(profileDirectory))
                    {
                        // 如果方案名称已存在，则添加时间戳
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        profile.Name = $"{profile.Name}_{timestamp}";
                        profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                    }
                    
                    // 创建方案目录
                    Directory.CreateDirectory(profileDirectory);
                    profile.FilePath = profileDirectory;
                    
                    // 复制KeySounds文件夹
                    var sourceKeySoundsDir = Path.Combine(tempDirectory, "KeySounds");
                    var destKeySoundsDir = Path.Combine(profileDirectory, "KeySounds");
                    if (Directory.Exists(sourceKeySoundsDir))
                    {
                        CopyDirectory(sourceKeySoundsDir, destKeySoundsDir);
                    }
                    
                    // 复制默认音效文件
                    var extensions = new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" };
                    foreach (var extension in extensions)
                    {
                        var defaultSoundFile = Path.Combine(tempDirectory, "DefaultSound" + extension);
                        if (File.Exists(defaultSoundFile))
                        {
                            var destDefaultSoundFile = Path.Combine(profileDirectory, "DefaultSound" + extension);
                            File.Copy(defaultSoundFile, destDefaultSoundFile);
                            profile.DefaultSound = destDefaultSoundFile;
                            break;
                        }
                    }
                    
                    // 修正KeySounds中的文件路径
                    foreach (var key in profile.KeySounds.Keys.ToList())
                    {
                        var soundPath = profile.KeySounds[key];
                        var fileName = Path.GetFileName(soundPath);
                        var newSoundPath = Path.Combine(destKeySoundsDir, fileName);
                        if (File.Exists(newSoundPath))
                        {
                            profile.KeySounds[key] = newSoundPath;
                        }
                        else
                        {
                            // 如果文件不存在，则移除该映射
                            profile.KeySounds.Remove(key);
                        }
                    }
                    
                    // 保存并加载新方案
                    SaveProfile(profile);
                    _profiles.Add(profile);
                    
                    return profile;
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入声音方案失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 复制目录及其内容
        /// </summary>
        /// <param name="sourceDir">源目录</param>
        /// <param name="destDir">目标目录</param>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            // 创建目标目录
            Directory.CreateDirectory(destDir);
            
            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile);
            }
            
            // 递归复制子目录
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDirectory);
            }
        }
        
        /// <summary>
        /// 获取按键的音效文件路径
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>音效文件路径</returns>
        public string GetKeySound(System.Windows.Input.Key key)
        {
            if (_currentProfile != null)
            {
                return _currentProfile.KeySounds.ContainsKey(key) 
                    ? _currentProfile.KeySounds[key] 
                    : _currentProfile.DefaultSound;
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 设置默认音效文件路径
        /// </summary>
        /// <param name="soundPath">音效文件路径</param>
        public void SetDefaultSound(string soundPath)
        {
            if (_currentProfile != null && File.Exists(soundPath))
            {
                try
                {
                    var extension = Path.GetExtension(soundPath);
                    var destFileName = $"DefaultSound{extension}";
                    var destFilePath = Path.Combine(_currentProfile.FilePath, destFileName);
                    
                    // 如果目标文件已存在，先删除它
                    if (File.Exists(destFilePath))
                    {
                        File.Delete(destFilePath);
                    }
                    
                    // 复制文件
                    File.Copy(soundPath, destFilePath, true);
                    
                    // 更新默认音效路径
                    _currentProfile.DefaultSound = destFilePath;
                    SaveProfile(_currentProfile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"设置默认音效失败: {ex.Message}");
                }
            }
        }
    }
}