using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace Xiuxian.Scripts.Services;

// Owns both hooks and their message queue on one dedicated thread. No Godot APIs here.
internal sealed class WindowsInputHookPump : IDisposable
{
    internal delegate IntPtr HookCallback(int code, IntPtr message, IntPtr data);
    private readonly HookCallback _keyboard;
    private readonly HookCallback _mouse;
    private readonly ManualResetEventSlim _ready = new();
    private Thread? _thread;
    private volatile uint _threadId;
    private volatile bool _stop;
    private volatile bool _active;
    private volatile string _error = "";
    public bool IsActive => _active;
    public string Error => _error;

    public WindowsInputHookPump(HookCallback keyboard, HookCallback mouse) { _keyboard = keyboard; _mouse = mouse; }

    public bool Start()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "Xiuxian input capture" };
        _thread.Start();
        if (_ready.Wait(3000) && _active) return true;
        if (_error.Length == 0) _error = "Input hook startup timed out.";
        Dispose(); return false;
    }

    private void Run()
    {
        IntPtr keyboard = IntPtr.Zero, mouse = IntPtr.Zero;
        try
        {
            _threadId = GetCurrentThreadId();
            PeekMessage(out _, IntPtr.Zero, 0, 0, 0); // Create queue before Stop can post WM_QUIT.
            if (_stop) return;
            IntPtr module = GetModuleHandle(null);
            keyboard = SetWindowsHookEx(13, _keyboard, module, 0);
            if (keyboard == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Keyboard hook failed");
            mouse = SetWindowsHookEx(14, _mouse, module, 0);
            if (mouse == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Mouse hook failed");
            _active = true; _ready.Set();
            while (!_stop)
            {
                int result = GetMessage(out var message, IntPtr.Zero, 0, 0);
                if (result == 0) break;
                if (result < 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Input message loop failed");
                TranslateMessage(ref message); DispatchMessage(ref message);
            }
        }
        catch (Exception ex) { _error = ex.Message; }
        finally
        {
            _active = false;
            if (mouse != IntPtr.Zero) UnhookWindowsHookEx(mouse);
            if (keyboard != IntPtr.Zero) UnhookWindowsHookEx(keyboard);
            _ready.Set();
        }
    }

    public void Dispose()
    {
        _stop = true;
        if (_threadId != 0) PostThreadMessage(_threadId, 0x0012, IntPtr.Zero, IntPtr.Zero);
        // Callbacks never wait for the game thread, so stopping cannot deadlock with Godot.
        _thread?.Join(3000);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Window;
        public uint Id;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X, Y;
        public uint Private;
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int type, HookCallback callback, IntPtr module, uint thread);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
    private static extern int GetMessage(out Message message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "PostThreadMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint thread, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);
}
