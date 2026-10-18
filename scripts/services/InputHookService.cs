using Godot;
using System;
using System.Runtime.InteropServices;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Windows 全局输入钩子服务
    /// 使用 SetWindowsHookEx 设置低级键盘和鼠标钩子
    /// 仅采集事件计数，不记录具体键值
    /// </summary>
    public partial class InputHookService : Node
    {
        [Signal]
        public delegate void HookStateChangedEventHandler(bool isActive);
        [Signal]
        public delegate void InputErrorEventHandler(string errorMessage);

        [Export] public bool AutoStart { get; set; } = true;
        [Export] public bool IsPaused { get; set; } = false;
        [Export] public bool EnableInAppFallback { get; set; } = true;
        [Export] public bool ForceGlobalCapture { get; set; } = true;
        [Export] public double GlobalHookRetryIntervalSeconds { get; set; } = 2.0;
        [Export] public float JoyAxisDeadzone { get; set; } = 0.35f;
        [Export] public float JoyAxisStep { get; set; } = 0.25f;
        [Export] public bool EnableJoyAxisCounting { get; set; } = false;

        public bool IsHookActive => _hookPump?.IsActive == true;
        public string CaptureStatus => IsPaused ? "已暂停采集"
            : IsHookActive ? "全局键鼠采集中（含其他软件）"
            : EnableInAppFallback ? "仅游戏窗口内采集（其他软件不会计数）" : "采集不可用";

        // 依赖：输入状态管理器
        [Export] public NodePath ActivityStatePath { get; set; } = "/root/InputActivityState";

        private InputActivityState _activityState;
        private bool _isHookActive = false;
        private double _retryCooldown;

        private WindowsInputHookPump? _hookPump;
        private readonly object _captureLock = new();
        private readonly ScrollInputAccumulator _globalScroll = new();
        private readonly ScrollInputAccumulator _fallbackScroll = new();

        // 鼠标位置追踪（用于计算移动距离）
        private Vector2I _lastMousePosition;
        private bool _hasLastMousePosition = false;
        private bool _warnedUnsupportedPlatform = false;
        private readonly System.Collections.Generic.Dictionary<string, float> _joyAxisSample = new();

        public override void _Ready()
        {
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-test") >= 0)
            {
                SetPaused(true);
                AutoStart = false;
                ForceGlobalCapture = false;
                SetProcessInput(false);
                return;
            }
            _activityState = GetNode<InputActivityState>(ActivityStatePath);
            if (_activityState == null)
            {
                GD.PushError("InputHookService: InputActivityState not found!");
                EmitSignal(SignalName.InputError, "InputActivityState not found");
                return;
            }

            ProcessMode = ProcessModeEnum.Always;
            SetProcessInput(true);

            if (AutoStart)
            {
                StartHook();
            }
        }

        public override void _Process(double delta)
        {
            if (_isHookActive && !IsHookActive)
            {
                string error = _hookPump?.Error ?? "Input capture stopped.";
                StopHook(); EmitSignal(SignalName.InputError, error);
            }
            if (!ForceGlobalCapture || IsPaused || IsHookActive || !IsWindowsPlatform())
            {
                return;
            }

            _retryCooldown -= delta;
            if (_retryCooldown > 0.0)
            {
                return;
            }

            _retryCooldown = Math.Max(0.2, GlobalHookRetryIntervalSeconds);
            StartHook();
        }

        public override void _ExitTree()
        {
            StopHook();
        }

        public override void _Input(InputEvent @event)
        {
            if (!EnableInAppFallback || IsPaused || _activityState == null)
            {
                return;
            }

            // Global Win hooks only cover keyboard/mouse; joypad must still be counted in-app.
            bool skipKeyboardMouseInApp = IsHookActive && IsWindowsPlatform();

            switch (@event)
            {
                case InputEventKey keyEvent when keyEvent.Pressed:
                    if (!skipKeyboardMouseInApp)
                    {
                        _activityState.RegisterKeyDown();
                    }
                    break;

                case InputEventMouseButton mouseButton when mouseButton.Pressed:
                    if (skipKeyboardMouseInApp)
                    {
                        break;
                    }

                    if (mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft or MouseButton.WheelRight)
                    {
                        int steps = _fallbackScroll.Add(mouseButton.Factor,
                            mouseButton.ButtonIndex is MouseButton.WheelLeft or MouseButton.WheelRight);
                        _activityState.RegisterMouseScroll(steps);
                    }
                    else
                    {
                        _activityState.RegisterMouseClick();
                    }
                    break;

                case InputEventMouseMotion motionEvent:
                    if (!skipKeyboardMouseInApp)
                    {
                        _activityState.RegisterMouseMove(motionEvent.Relative.Length());
                    }
                    break;

                case InputEventJoypadButton joyButton when joyButton.Pressed:
                    _activityState.RegisterJoypadButton();
                    break;

                case InputEventJoypadMotion joyMotion:
                    if (EnableJoyAxisCounting)
                    {
                        HandleJoypadMotion(joyMotion);
                    }
                    break;
            }
        }

        private void HandleJoypadMotion(InputEventJoypadMotion joyMotion)
        {
            float value = joyMotion.AxisValue;
            float absValue = Mathf.Abs(value);
            string key = $"{joyMotion.Device}:{(int)joyMotion.Axis}";
            float previous = _joyAxisSample.TryGetValue(key, out float prev) ? prev : 0.0f;

            // Only count meaningful stick/trigger changes to avoid per-frame flooding.
            if (absValue < JoyAxisDeadzone)
            {
                _joyAxisSample[key] = 0.0f;
                return;
            }

            float delta = Mathf.Abs(absValue - previous);
            if (previous <= 0.0f || delta >= JoyAxisStep)
            {
                _activityState.RegisterJoypadAxisInput();
                _joyAxisSample[key] = absValue;
                return;
            }

            _joyAxisSample[key] = absValue;
        }

        /// <summary>
        /// 启动全局钩子
        /// </summary>
        public void StartHook()
        {
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-test") >= 0) return;
            if (IsHookActive) return;
            if (!IsWindowsPlatform())
            {
                if (!_warnedUnsupportedPlatform)
                {
                    _warnedUnsupportedPlatform = true;
                    EmitSignal(SignalName.InputError, "Global hooks are available only on Windows; using in-app capture.");
                }
                return;
            }
            _hookPump?.Dispose();
            lock (_captureLock) { _globalScroll.Reset(); _hasLastMousePosition = false; }
            _fallbackScroll.Reset();
            _hookPump = new(KeyboardHookCallback, MouseHookCallback);
            if (!_hookPump.Start())
            {
                EmitSignal(SignalName.InputError, _hookPump.Error);
                _retryCooldown = Math.Max(0.2, GlobalHookRetryIntervalSeconds);
                return;
            }
            _isHookActive = true; _retryCooldown = 0;
            EmitSignal(SignalName.HookStateChanged, true);
            GD.Print("InputHookService: Global hooks started on dedicated capture thread");
        }

        public void StopHook()
        {
            _hookPump?.Dispose(); _hookPump = null;
            bool wasActive = _isHookActive; _isHookActive = false;
            _retryCooldown = Math.Max(0.2, GlobalHookRetryIntervalSeconds);
            if (wasActive) EmitSignal(SignalName.HookStateChanged, false);
        }

        /// <summary>
        /// 暂停/恢复采集
        /// </summary>
        public void SetPaused(bool paused)
        {
            lock (_captureLock)
            {
                IsPaused = paused;
                _globalScroll.Reset(); _hasLastMousePosition = false;
            }
            _fallbackScroll.Reset();
            _joyAxisSample.Clear();
            // Already captured inputs settle normally; never discard their statistics on pause.
            GD.Print($"InputHookService: {(paused ? "Paused" : "Resumed")}");
        }

        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        /// <summary>
        /// 键盘钩子回调
        /// </summary>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            lock (_captureLock)
            if (nCode >= 0 && !IsPaused && _activityState != null)
            {
                // wParam: WM_KEYDOWN = 256, WM_KEYUP = 257, WM_SYSKEYDOWN = 260, WM_SYSKEYUP = 261
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    // 不记录具体键值 (lParam 包含虚拟键码，但我们不存储它)
                    _activityState.RegisterKeyDown();
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        /// <summary>
        /// 鼠标钩子回调
        /// </summary>
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            lock (_captureLock)
            if (nCode >= 0 && !IsPaused && _activityState != null)
            {
                // 解析鼠标结构
                var mouseInfo = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var currentPos = new Vector2I(mouseInfo.pt.x, mouseInfo.pt.y);

                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                    case WM_RBUTTONDOWN:
                    case WM_MBUTTONDOWN:
                    case WM_XBUTTONDOWN:
                        _activityState.RegisterMouseClick();
                        break;

                    case WM_MOUSEWHEEL:
                    case WM_MOUSEHWHEEL:
                        int delta = (short)((mouseInfo.mouseData >> 16) & 0xFFFF);
                        int steps = _globalScroll.AddWindowsDelta(delta, (int)wParam == WM_MOUSEHWHEEL);
                        _activityState.RegisterMouseScroll(steps);
                        break;

                    case WM_MOUSEMOVE:
                        if (_hasLastMousePosition)
                        {
                            double distance = _lastMousePosition.DistanceTo(currentPos);
                            _activityState.RegisterMouseMove(distance);
                        }
                        _lastMousePosition = currentPos;
                        _hasLastMousePosition = true;
                        break;
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        #region Win32 API

        private static bool IsWindowsPlatform()
        {
            return OS.GetName() == "Windows";
        }

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}
