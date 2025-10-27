using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Threading;

namespace KeySound2;

public class KeyboardHook : IDisposable
{
    private IntPtr _hookID = IntPtr.Zero;
    private KeyboardHookProc _procDelegate; // 保存委托引用以防止被垃圾回收
    
    public event EventHandler<KeyPressedEventArgs> OnKeyPressed;
    
    public KeyboardHook()
    {
        // 显式保存委托引用以防止被垃圾回收
        _procDelegate = LowLevelKeyboardProc;
        _hookID = SetHook(_procDelegate);
    }
    
    public void Start()
    {
        // 钩子已在构造函数中设置
    }
    
    public void Stop()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }
    
    private IntPtr SetHook(KeyboardHookProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }
    
    private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            try
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                
                // 使用线程池执行事件处理，避免在钩子回调中执行复杂操作
                ThreadPool.QueueUserWorkItem(_ => {
                    try
                    {
                        // 检查事件是否有订阅者
                        if (OnKeyPressed != null)
                        {
                            OnKeyPressed(this, new KeyPressedEventArgs(key));
                        }
                    }
                    catch (Exception ex)
                    {
                        // 静默处理事件处理异常，避免影响键盘输入
                        System.Diagnostics.Debug.WriteLine($"处理键盘事件失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                // 静默处理键盘事件处理异常，避免影响键盘输入
                System.Diagnostics.Debug.WriteLine($"处理键盘事件失败: {ex.Message}");
            }
        }
        // 确保始终调用下一个钩子
        try
        {
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        catch
        {
            // 即使CallNextHookEx失败，也要确保返回一个合理的值
            return IntPtr.Zero;
        }
    }
    
    ~KeyboardHook()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        Stop();
        _procDelegate = null; // 清除委托引用
    }
    
    // 修复CS0102错误：重命名委托以避免与方法名冲突
    private delegate IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookProc lpfn, IntPtr hMod, uint dwThreadId);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104; // 添加对系统按键的支持，包括屏幕键盘
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
}