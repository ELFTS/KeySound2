using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace KeySound2
{
    /// <summary>
    /// 声音设置类，用于管理按键音效和声音方案
    /// </summary>
    public class SoundSettings
    {
        // 存储每个按键的音效文件路径
        private Dictionary<Key, string> _keySounds = new Dictionary<Key, string>();
        
        // 存储每个按键的音量设置(0.0-1.0)
        private Dictionary<Key, float> _keyVolumes = new Dictionary<Key, float>();
        
        // 默认音效路径
        private string _defaultSoundPath = "";
        public string DefaultSoundPath
        {
            get { return _defaultSoundPath; }
            set
            {
                if (!string.IsNullOrEmpty(value) && File.Exists(value))
                {
                    _defaultSoundPath = value;
                }
                else
                {
                    _defaultSoundPath = "";
                }
            }
        }
        
        // 声音方案名称
        public string SoundSchemeName { get; set; } = "默认方案";
        
        // 声音方案路径
        public string SoundSchemePath { get; set; } = "";
        
        /// <summary>
        /// 为指定按键设置音效
        /// </summary>
        /// <param name="key">按键</param>
        /// <param name="soundPath">音效文件路径</param>
        public void SetSoundForKey(Key key, string soundPath)
        {
            if (key == Key.None)
                return;
                
            if (string.IsNullOrEmpty(soundPath))
            {
                // 如果音效路径为空，移除该按键的设置
                _keySounds.Remove(key);
                System.Diagnostics.Debug.WriteLine($"已移除按键 {key} 的音效设置");
            }
            else
            {
                // 设置按键音效
                _keySounds[key] = soundPath;
                System.Diagnostics.Debug.WriteLine($"已为按键 {key} 设置音效: {soundPath}");
            }
        }
        
        /// <summary>
        /// 清除指定按键的音效设置
        /// </summary>
        /// <param name="key">按键</param>
        public void ClearSoundForKey(Key key)
        {
            if (key == Key.None)
                return;
                
            _keySounds.Remove(key);
            System.Diagnostics.Debug.WriteLine($"已清除按键 {key} 的音效设置");
        }
        
        /// <summary>
        /// 为指定按键设置音量
        /// </summary>
        /// <param name="key">按键</param>
        /// <param name="volume">音量(0.0-1.0)</param>
        public void SetVolumeForKey(Key key, float volume)
        {
            if (key == Key.None)
                return;
                
            _keyVolumes[key] = Math.Max(0.0f, Math.Min(1.0f, volume));
        }
        
        /// <summary>
        /// 获取指定按键的音效路径
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>音效文件路径</returns>
        public string GetSoundPathForKey(Key key)
        {
            System.Diagnostics.Debug.WriteLine($"获取按键 {key} 的音效路径");
            
            // 检查是否为该按键设置了特定的音效
            if (_keySounds.ContainsKey(key))
            {
                string soundPath = _keySounds[key];
                System.Diagnostics.Debug.WriteLine($"按键 {key} 有特定音效: {soundPath}");
                
                // 如果设置了音效路径且文件存在，则返回该路径
                if (!string.IsNullOrEmpty(soundPath) && File.Exists(soundPath))
                {
                    System.Diagnostics.Debug.WriteLine($"按键 {key} 的音效文件存在: {soundPath}");
                    return soundPath;
                }
                // 如果设置了音效路径但文件不存在，继续检查默认音效
                else if (!string.IsNullOrEmpty(soundPath))
                {
                    System.Diagnostics.Debug.WriteLine($"按键 {key} 的音效文件不存在: {soundPath}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"按键 {key} 没有特定音效设置");
            }
            
            // 如果有设置默认音效且文件存在，返回默认音效
            if (!string.IsNullOrEmpty(DefaultSoundPath) && File.Exists(DefaultSoundPath))
            {
                System.Diagnostics.Debug.WriteLine($"使用默认音效文件为按键 {key}: {DefaultSoundPath}");
                return DefaultSoundPath;
            }
            
            // 如果没有可用的音效文件，返回空字符串
            System.Diagnostics.Debug.WriteLine($"按键 {key} 没有可用的音效文件");
            return "";
        }
        
        /// <summary>
        /// 获取指定按键的音量
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>音量(0.0-1.0)</returns>
        public float GetVolumeForKey(Key key)
        {
            // 检查是否为该按键设置了特定的音量
            if (_keyVolumes.ContainsKey(key))
            {
                return _keyVolumes[key];
            }
            
            // 如果没有设置特定音量，返回默认音量(0.5)
            return 0.5f;
        }
        
        /// <summary>
        /// 从声音方案目录加载设置
        /// </summary>
        /// <param name="schemePath">方案目录路径</param>
        public void LoadFromScheme(string schemePath)
        {
            if (string.IsNullOrEmpty(schemePath) || !Directory.Exists(schemePath))
            {
                System.Diagnostics.Debug.WriteLine($"方案目录不存在: {schemePath}");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"开始加载声音方案: {schemePath}");
            
            // 清空当前设置
            _keySounds.Clear();
            _keyVolumes.Clear();
            
            // 更新方案路径和名称
            SoundSchemePath = schemePath;
            SoundSchemeName = new DirectoryInfo(schemePath).Name;
            System.Diagnostics.Debug.WriteLine($"方案名称: {SoundSchemeName}");
            
            // 查找方案目录中的配置文件
            string configPath = Path.Combine(schemePath, "scheme.json");
            System.Diagnostics.Debug.WriteLine($"查找配置文件: {configPath}");
            
            if (File.Exists(configPath))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("发现配置文件，开始解析");
                    string json = File.ReadAllText(configPath);
                    var schemeSettings = System.Text.Json.JsonSerializer.Deserialize<SchemeSettings>(json);
                    if (schemeSettings != null)
                    {
                        System.Diagnostics.Debug.WriteLine("配置文件解析成功");
                        
                        // 加载按键音效
                        if (schemeSettings.KeySounds != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"开始加载 {schemeSettings.KeySounds.Count} 个按键音效");
                            
                            foreach (var kvp in schemeSettings.KeySounds)
                            {
                                string soundPath = Path.Combine(schemePath, kvp.Value);
                                System.Diagnostics.Debug.WriteLine($"按键 {kvp.Key} 音效路径: {soundPath}");
                                
                                if (File.Exists(soundPath))
                                {
                                    SetSoundForKey(kvp.Key, soundPath);
                                    System.Diagnostics.Debug.WriteLine($"按键 {kvp.Key} 音效设置成功");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"按键 {kvp.Key} 音效文件不存在: {soundPath}");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("配置文件中没有按键音效设置");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("配置文件解析结果为空");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"解析配置文件失败: {ex.Message}");
                    // 如果配置文件解析失败，继续使用目录中的音频文件
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("未找到配置文件");
            }
            
            // 遍历方案目录中的音频文件，自动为按键分配音效
            try
            {
                string[] audioFiles = Directory.GetFiles(schemePath, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".aac", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".wma", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                    
                System.Diagnostics.Debug.WriteLine($"发现 {audioFiles.Length} 个音频文件");
                
                // 为每个音频文件尝试分配给一个按键
                for (int i = 0; i < Math.Min(audioFiles.Length, Enum.GetValues(typeof(Key)).Length - 1); i++)
                {
                    Key key = (Key)Enum.GetValues(typeof(Key)).GetValue(i + 1); // 跳过Key.None
                    if (!_keySounds.ContainsKey(key)) // 只有在没有设置的情况下才自动分配
                    {
                        SetSoundForKey(key, audioFiles[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"遍历音频文件时出错: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("声音方案加载完成");
        }
        
        /// <summary>
        /// 保存设置到声音方案目录
        /// </summary>
        /// <param name="schemePath">方案目录路径</param>
        public void SaveToScheme(string schemePath)
        {
            if (string.IsNullOrEmpty(schemePath))
                return;
                
            // 确保目录存在
            if (!Directory.Exists(schemePath))
            {
                Directory.CreateDirectory(schemePath);
            }
            
            // 更新方案路径和名称
            SoundSchemePath = schemePath;
            SoundSchemeName = new DirectoryInfo(schemePath).Name;
            
            // 创建方案设置对象
            var schemeSettings = new SchemeSettings
            {
                KeySounds = new Dictionary<Key, string>()
            };
            
            // 处理按键音效
            foreach (var kvp in _keySounds)
            {
                if (!string.IsNullOrEmpty(kvp.Value) && File.Exists(kvp.Value))
                {
                    string fileName = Path.GetFileName(kvp.Value);
                    string targetPath = Path.Combine(schemePath, fileName);
                    
                    // 复制文件到方案目录
                    if (kvp.Value != targetPath)
                    {
                        File.Copy(kvp.Value, targetPath, true);
                    }
                    
                    schemeSettings.KeySounds[kvp.Key] = fileName;
                }
            }
            
            // 保存配置文件
            string configPath = Path.Combine(schemePath, "scheme.json");
            string json = JsonSerializer.Serialize(schemeSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
    }
    
    /// <summary>
    /// 声音方案配置类
    /// </summary>
    public class SchemeSettings
    {
        public Dictionary<Key, string> KeySounds { get; set; } = new Dictionary<Key, string>();
    }
}