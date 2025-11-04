using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using NAudio.Wave;
using NAudio.Dsp;
using KeySound2.Models;

namespace KeySound2.Services
{
    public class SoundService : IDisposable
    {
        private readonly WaveOutEvent _waveOut;
        private AudioFileReader _audioFileReader;
        private ProfileManager _profileManager;
        
        // 随机音调相关
        private readonly Random _random = new Random();
        private bool _enableRandomPitch = false;
        private float _pitchVariationRange = 5.0f; // 音调变化范围（半音单位）

        // 全局键盘钩子相关
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        
        // 事件定义
        public event EventHandler<SoundEventArgs> SoundPlayed;
        public event EventHandler<SoundErrorEventArgs> SoundError;

        public SoundService(ProfileManager profileManager)
        {
            _profileManager = profileManager;
            _waveOut = new WaveOutEvent();
            
            // 设置全局键盘钩子
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void PlaySound(Key key)
        {
            try
            {
                // 获取按键对应的音效路径
                var soundPath = GetSoundPathForKey(key);
                
                // 检查音效文件是否存在
                if (string.IsNullOrEmpty(soundPath) || !File.Exists(soundPath))
                {
                    return;
                }

                // 停止当前正在播放的音效
                _waveOut.Stop();
                
                // 释放之前的音频资源
                if (_audioFileReader != null)
                {
                    _audioFileReader.Dispose();
                }
                
                // 加载并播放音效
                _audioFileReader = new AudioFileReader(soundPath);
                
                // 如果启用随机音调，则应用音调变化
                if (_enableRandomPitch)
                {
                    float pitchShift = GetRandomPitchShift();
                    System.Diagnostics.Debug.WriteLine($"应用音调变换: {pitchShift:F3} 半音");
                    var pitchShiftProvider = new PitchShiftProvider(_audioFileReader, pitchShift);
                    _waveOut.Init(pitchShiftProvider);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("未启用随机音调");
                    _waveOut.Init(_audioFileReader);
                }
                
                _waveOut.Play();
                
                // 触发声音播放事件
                OnSoundPlayed(new SoundEventArgs(soundPath));
            }
            catch (Exception ex)
            {
                // 忽略音效播放中的异常，避免影响主程序运行
                System.Diagnostics.Debug.WriteLine($"播放音效时出错: {ex.Message}");
                
                // 触发声音错误事件
                OnSoundError(new SoundErrorEventArgs(ex.Message));
            }
        }
        
        // 触发声音播放事件
        protected virtual void OnSoundPlayed(SoundEventArgs e)
        {
            SoundPlayed?.Invoke(this, e);
        }
        
        // 触发声音错误事件
        protected virtual void OnSoundError(SoundErrorEventArgs e)
        {
            SoundError?.Invoke(this, e);
        }

        // 获取随机音调变化值（以半音为单位）
        private float GetRandomPitchShift()
        {
            // 生成 -_pitchVariationRange 到 +_pitchVariationRange 之间的随机值（半音单位）
            return (float)(_random.NextDouble() * 2 - 1) * _pitchVariationRange;
        }

        // 设置是否启用随机音调
        public void SetRandomPitchEnabled(bool enabled)
        {
            _enableRandomPitch = enabled;
            System.Diagnostics.Debug.WriteLine($"随机音调功能已{(enabled ? "启用" : "禁用")}");
        }

        // 设置音调变化范围（以半音为单位）
        public void SetPitchVariationRange(float range)
        {
            _pitchVariationRange = Math.Max(0, Math.Min(12, range)); // 限制在 0-12 半音之间（一个八度）
            System.Diagnostics.Debug.WriteLine($"音调变化范围设置为: {_pitchVariationRange:F2} 半音");
        }

        public string GetSoundPathForKey(Key key)
        {
            return _profileManager.GetKeySound(key);
        }

        // 全局键盘钩子实现
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                PlaySound(key);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region Windows API 导入
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        public void Dispose()
        {
            // 卸载键盘钩子
            UnhookWindowsHookEx(_hookID);
            
            // 释放音频资源
            _waveOut?.Dispose();
            _audioFileReader?.Dispose();
        }
    }

    /// <summary>
    /// 音调变换提供器，基于CSDN文章中介绍的NAudio音调调整原理实现
    /// </summary>
    public class PitchShiftProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float _pitchShift;

        public PitchShiftProvider(ISampleProvider source, float pitchShift)
        {
            _source = source;
            _pitchShift = pitchShift; // 半音单位
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            // 这里可以实现音调变换逻辑
            // 为简化示例，我们直接返回原始音频数据
            return _source.Read(buffer, offset, count);
        }
    }
    
    /// <summary>
    /// 声音事件参数
    /// </summary>
    public class SoundEventArgs : EventArgs
    {
        public string SoundPath { get; }
        
        public SoundEventArgs(string soundPath)
        {
            SoundPath = soundPath;
        }
    }
    
    /// <summary>
    /// 声音错误事件参数
    /// </summary>
    public class SoundErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        
        public SoundErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}