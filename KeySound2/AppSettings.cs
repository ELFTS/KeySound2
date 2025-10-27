using System;
using System.IO;
using System.Text.Json;

namespace KeySound2
{
    /// <summary>
    /// 程序设置类
    /// </summary>
    public class AppSettings
    {
        // 默认值
        public bool StartMinimized { get; set; } = false;
        public double DefaultVolume { get; set; } = 50.0;
        public bool ShowTrayIcon { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        
        /// <summary>
        /// 保存设置到配置文件
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存程序设置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从配置文件加载设置
        /// </summary>
        /// <returns>AppSettings实例</returns>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载程序设置失败: {ex.Message}");
            }
            
            // 如果文件不存在或加载失败，返回默认设置
            return new AppSettings();
        }
    }
}