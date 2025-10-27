using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

namespace KeySound2;

/// <summary>
/// 音效管理器，用于播放和管理不同按键的音效
/// </summary>
public class SoundManager : IDisposable
{
    private AudioManager _audioManager;
    private SoundSettings _settings;
    private bool _isDisposed = false; // 添加disposed标志以防止重复释放
    
    public SoundManager(SoundSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audioManager = new AudioManager();
    }
    
    /// <summary>
    /// 播放指定按键的音效
    /// </summary>
    /// <param name="key">按键</param>
    public void PlaySound(Key key)
    {
        // 检查对象是否已被释放
        if (_isDisposed)
        {
            System.Diagnostics.Debug.WriteLine("SoundManager已释放，无法播放音效");
            return;
        }
            
        try
        {
            // 检查设置和音频管理器是否有效
            if (_settings == null || _audioManager == null)
            {
                System.Diagnostics.Debug.WriteLine("音效设置或音频管理器为空");
                return;
            }
                
            string soundPath = _settings.GetSoundPathForKey(key);
            
            // 检查音效路径是否有效
            if (string.IsNullOrEmpty(soundPath))
            {
                System.Diagnostics.Debug.WriteLine($"未找到按键 {key} 的音效路径");
                return;
            }
            
            if (!File.Exists(soundPath))
            {
                System.Diagnostics.Debug.WriteLine($"音效文件不存在: {soundPath}");
                return;
            }
            
            string keyName = key.ToString();
            float volume = _settings.GetVolumeForKey(key);
            
            // 限制音量范围在0.0到1.0之间
            volume = Math.Max(0.0f, Math.Min(1.0f, volume));
            
            System.Diagnostics.Debug.WriteLine($"播放音效: {keyName}, 路径: {soundPath}, 音量: {volume}");
            _audioManager.PlaySound(keyName, soundPath, volume);
        }
        catch (Exception ex)
        {
            // 静默处理音效播放错误，避免影响键盘输入
            System.Diagnostics.Debug.WriteLine($"播放音效失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 更新音效设置
    /// </summary>
    /// <param name="settings">新的音效设置</param>
    public void UpdateSettings(SoundSettings settings)
    {
        // 检查对象是否已被释放
        if (_isDisposed)
        {
            System.Diagnostics.Debug.WriteLine("SoundManager已释放，无法更新设置");
            return;
        }
            
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        System.Diagnostics.Debug.WriteLine("音效设置已更新");
    }
    
    public void Dispose()
    {
        // 检查对象是否已被释放
        if (_isDisposed)
            return;
            
        _isDisposed = true;
        
        try
        {
            _audioManager?.Dispose();
        }
        catch (Exception ex)
        {
            // 静默处理音频管理器释放异常
            System.Diagnostics.Debug.WriteLine($"释放音频管理器失败: {ex.Message}");
        }
        finally
        {
            _audioManager = null;
            _settings = null;
        }
    }
}