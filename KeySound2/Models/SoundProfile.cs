using System.Collections.Generic;
using System.Windows.Input;

namespace KeySound2.Models
{
    /// <summary>
    /// 声音方案模型
    /// </summary>
    public class SoundProfile
    {
        /// <summary>
        /// 方案名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 按键到音效文件路径的映射
        /// </summary>
        public Dictionary<Key, string> KeySounds { get; set; }
        
        /// <summary>
        /// 默认音效文件路径
        /// </summary>
        public string DefaultSound { get; set; }
        
        /// <summary>
        /// 方案文件路径
        /// </summary>
        public string FilePath { get; set; }
        
        public SoundProfile()
        {
            Name = "默认方案";
            KeySounds = new Dictionary<Key, string>();
            DefaultSound = string.Empty;
            FilePath = string.Empty;
        }
        
        public SoundProfile(string name) : this()
        {
            Name = name;
        }
    }
}