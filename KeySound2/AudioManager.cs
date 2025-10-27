using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Threading;

namespace KeySound2;

/// <summary>
/// 音频管理器，支持MP3格式和音量控制
/// </summary>
public class AudioManager : IDisposable
{
    private readonly object _lockObject = new object(); // 添加锁对象以确保线程安全
    private bool _isDisposed = false; // 添加disposed标志以防止重复释放
    
    /// <summary>
    /// 播放指定按键的音频文件
    /// </summary>
    /// <param name="keyName">按键名称</param>
    /// <param name="audioFilePath">音频文件路径</param>
    /// <param name="volume">音量(0.0-1.0)</param>
    public void PlaySound(string keyName, string audioFilePath, float volume = 0.5f)
    {
        // 检查对象是否已被释放
        if (_isDisposed)
        {
            System.Diagnostics.Debug.WriteLine("AudioManager已释放，无法播放音频");
            return;
        }
            
        // 验证输入参数
        if (string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(audioFilePath))
        {
            System.Diagnostics.Debug.WriteLine("音频播放参数无效");
            return;
        }
            
        // 检查文件是否存在
        if (!File.Exists(audioFilePath))
        {
            System.Diagnostics.Debug.WriteLine($"音频文件不存在: {audioFilePath}");
            return;
        }
            
        System.Diagnostics.Debug.WriteLine($"开始播放音频: {keyName}, 路径: {audioFilePath}, 音量: {volume}");
        
        // 使用线程池执行播放操作，避免阻塞调用线程（特别是键盘钩子线程）
        ThreadPool.QueueUserWorkItem(_ => PlaySoundInternal(keyName, audioFilePath, volume));
    }
    
    /// <summary>
    /// 内部播放方法，在线程池线程中执行
    /// </summary>
    /// <param name="keyName">按键名称</param>
    /// <param name="audioFilePath">音频文件路径</param>
    /// <param name="volume">音量(0.0-1.0)</param>
    private void PlaySoundInternal(string keyName, string audioFilePath, float volume)
    {
        // 检查对象是否已被释放
        if (_isDisposed)
        {
            System.Diagnostics.Debug.WriteLine("AudioManager已释放，无法播放音频(内部)");
            return;
        }
            
        try
        {
            System.Diagnostics.Debug.WriteLine($"开始内部播放音频: {keyName}");
            
            // 为每个播放操作创建唯一的键名，避免并发冲突
            string uniqueKeyName = $"{keyName}_{Guid.NewGuid()}";
            
            // 创建音频读取器
            AudioFileReader audioReader = null;
            IWavePlayer wavePlayer = null; // 更改类型为IWavePlayer接口
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"创建音频读取器: {audioFilePath}");
                audioReader = new AudioFileReader(audioFilePath);
                audioReader.Volume = Math.Max(0.0f, Math.Min(1.0f, volume));
                System.Diagnostics.Debug.WriteLine($"音频读取器创建成功，音量: {audioReader.Volume}");
                
                // 尝试使用WASAPI输出方式，如果失败则回退到WaveOutEvent
                try
                {
                    System.Diagnostics.Debug.WriteLine("尝试使用WASAPI输出");
                    wavePlayer = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 20); // 20ms的缓冲区
                    System.Diagnostics.Debug.WriteLine("WASAPI输出创建成功");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WASAPI输出创建失败，回退到WaveOutEvent: {ex.Message}");
                    // WASAPI不可用时回退到WaveOutEvent
                    wavePlayer = new WaveOutEvent
                    {
                        DeviceNumber = -1 // -1表示默认设备
                    };
                    System.Diagnostics.Debug.WriteLine("WaveOutEvent输出创建成功");
                }
                
                System.Diagnostics.Debug.WriteLine("初始化播放器");
                wavePlayer.Init(audioReader);
                System.Diagnostics.Debug.WriteLine("播放器初始化成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建播放器过程中出现异常: {ex.Message}");
                // 如果创建过程中出现异常，确保释放已创建的资源
                try
                {
                    audioReader?.Dispose();
                }
                catch (Exception disposeEx) { 
                    System.Diagnostics.Debug.WriteLine($"释放音频读取器失败: {disposeEx.Message}"); 
                }
                
                try
                {
                    wavePlayer?.Dispose();
                }
                catch (Exception disposeEx) { 
                    System.Diagnostics.Debug.WriteLine($"释放播放器失败: {disposeEx.Message}"); 
                }
                
                System.Diagnostics.Debug.WriteLine($"创建音频播放器失败: {ex.Message}");
                return;
            }
            
            // 播放完成事件
            wavePlayer.PlaybackStopped += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"播放完成: {uniqueKeyName}");
                try
                {
                    // 播放完成后释放资源
                    try
                    {
                        wavePlayer?.Dispose();
                    }
                    catch (Exception ex) { 
                        System.Diagnostics.Debug.WriteLine($"释放播放器失败: {ex.Message}"); 
                    }
                    
                    try
                    {
                        audioReader?.Dispose();
                    }
                    catch (Exception ex) { 
                        System.Diagnostics.Debug.WriteLine($"释放音频读取器失败: {ex.Message}"); 
                    }
                }
                catch (Exception ex)
                {
                    // 静默处理资源释放错误
                    System.Diagnostics.Debug.WriteLine($"释放音频资源失败: {ex.Message}");
                }
            };
            
            // 开始播放
            System.Diagnostics.Debug.WriteLine($"开始播放: {uniqueKeyName}");
            wavePlayer.Play();
            System.Diagnostics.Debug.WriteLine($"播放已启动: {uniqueKeyName}");
            
            // 短暂延迟以确保播放开始后再返回
            Thread.Sleep(10);
        }
        catch (Exception ex)
        {
            // 静默处理音频播放错误，避免影响键盘输入
            System.Diagnostics.Debug.WriteLine($"播放音频失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 设置指定按键音频的音量
    /// </summary>
    /// <param name="keyName">按键名称</param>
    /// <param name="volume">音量(0.0-1.0)</param>
    public void SetVolume(string keyName, float volume)
    {
        // 音量设置功能在当前实现中不再适用，因为我们为每次播放创建独立的资源
        System.Diagnostics.Debug.WriteLine("音量设置功能在当前实现中不可用");
    }

    /// <summary>
    /// 停止指定按键的音频播放
    /// </summary>
    /// <param name="keyName">按键名称</param>
    public void StopSound(string keyName)
    {
        // 停止播放功能在当前实现中不再适用，因为我们为每次播放创建独立的资源
        System.Diagnostics.Debug.WriteLine("停止播放功能在当前实现中不可用");
    }

    /// <summary>
    /// 停止所有音频播放
    /// </summary>
    public void StopAllSounds()
    {
        // 停止所有播放功能在当前实现中不再适用，因为我们为每次播放创建独立的资源
        System.Diagnostics.Debug.WriteLine("停止所有播放功能在当前实现中不可用");
    }
    
    public void Dispose()
    {
        // 检查对象是否已被释放
        if (_isDisposed)
            return;
            
        lock (_lockObject)
        {
            // 双重检查
            if (_isDisposed)
                return;
                
            _isDisposed = true;
            
            // 当前实现中不需要特殊清理，因为每个播放操作都管理自己的资源
            System.Diagnostics.Debug.WriteLine("AudioManager资源已释放");
        }
    }
}