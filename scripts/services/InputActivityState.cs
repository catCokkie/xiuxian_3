using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Event-driven input aggregator with anti-abuse guards:
    /// 1) aggregate input into short batches
    /// 2) clamp effective discrete inputs with a 60s sliding window hard cap
    /// </summary>
    public partial class InputActivityState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void ActivityTickEventHandler(double apThisBatch, double apFinal);

        [Signal]
        public delegate void InputBatchTickEventHandler(int inputEventsThisBatch, double apFinal);

        // Latest effective batch counters (after hard cap filtering).
        public int KeyDownCount { get; private set; }
        public int MouseClickCount { get; private set; }
        public int MouseScrollSteps { get; private set; }
        public double MouseMoveDistancePx { get; private set; }
        public int JoypadButtonCount { get; private set; }
        public int JoypadAxisInputCount { get; private set; }

        // Lifetime counters (raw captured input, unaffected by the hard cap).
        public long TotalKeyDownCount { get; private set; }
        public long TotalMouseClickCount { get; private set; }
        public long TotalMouseScrollSteps { get; private set; }
        public double TotalMouseMoveDistancePx { get; private set; }
        public long TotalJoypadButtonCount { get; private set; }
        public long TotalJoypadAxisInputCount { get; private set; }
        public double TotalActiveSeconds { get; private set; }
        public double SecondsSinceLastInput { get; private set; }
        public double SecondsSinceLastInputBeforeLatestBatch { get; private set; }

        // AP results
        public double ApThisSecond { get; private set; }
        public double ApFinal { get; private set; }
        public double ApAccumulator { get; private set; }
        public int InputEventsThisSecond { get; private set; }

        [Export] public double KeyDownWeight { get; set; } = 1.0;
        [Export] public double MouseClickWeight { get; set; } = 1.2;
        [Export] public double ScrollStepWeight { get; set; } = 0.4;
        [Export] public double MovePxDivider { get; set; } = 600.0;
        [Export] public double JoypadButtonWeight { get; set; } = 1.0;
        [Export] public double JoypadAxisWeight { get; set; } = 0.8;
        [Export] public double InputWindowSeconds { get; set; } = GameBalanceConstants.InputAntiAbuse.WindowSeconds;
        [Export] public int MaxInputPerMinute { get; set; } = GameBalanceConstants.InputAntiAbuse.MaxInputPerMinute;
        [Export] public double AccumulatorDrainPerSecond { get; set; } = 0.6;

        private const int MaxRollingQueueSize = 10000;

        private readonly object _pendingLock = new();
        private readonly Queue<(double time, int acceptedInputCount)> _acceptedInputWindow = new();

        private int _pendingKeyDown;
        private int _pendingMouseClick;
        private int _pendingMouseScroll;
        private double _pendingMouseMove;
        private int _pendingJoypadButton;
        private int _pendingJoypadAxis;

        private int _acceptedInputCountInWindow;
        private double _runtimeSeconds;
        private double _pendingIdleSecondsBeforeBatch;
        private bool _hasPendingIdleSnapshot;

        public override void _Process(double delta)
        {
            _runtimeSeconds += delta;
            SecondsSinceLastInput += delta;
            PruneAcceptedInputWindow();

            DrainPendingInput(
                out int keyBatch,
                out int clickBatch,
                out int scrollBatch,
                out double moveBatch,
                out int joypadButtonBatch,
                out int joypadAxisBatch);

            InputActivityRules.DiscreteInputBatch rawDiscreteBatch = new(
                keyBatch,
                clickBatch,
                scrollBatch,
                joypadButtonBatch,
                joypadAxisBatch);
            if (rawDiscreteBatch.TotalCount <= 0 && moveBatch <= 0.0)
            {
                return;
            }

            int remainingAllowance = InputActivityRules.CalculateRemainingWindowAllowance(
                _acceptedInputCountInWindow,
                MaxInputPerMinute);
            InputActivityRules.DiscreteInputBatch acceptedDiscreteBatch = InputActivityRules.ClampDiscreteInputBatch(
                rawDiscreteBatch,
                remainingAllowance);
            int acceptedInputCount = acceptedDiscreteBatch.TotalCount;
            double acceptedAp = InputActivityRules.CalculateRawAp(
                acceptedDiscreteBatch,
                moveBatch,
                KeyDownWeight,
                MouseClickWeight,
                ScrollStepWeight,
                MovePxDivider,
                JoypadButtonWeight,
                JoypadAxisWeight);
            if (acceptedInputCount <= 0 && acceptedAp <= 0.0)
            {
                return;
            }

            KeyDownCount = acceptedDiscreteBatch.KeyDownCount;
            MouseClickCount = acceptedDiscreteBatch.MouseClickCount;
            MouseScrollSteps = acceptedDiscreteBatch.MouseScrollSteps;
            MouseMoveDistancePx = moveBatch;
            JoypadButtonCount = acceptedDiscreteBatch.JoypadButtonCount;
            JoypadAxisInputCount = acceptedDiscreteBatch.JoypadAxisInputCount;
            InputEventsThisSecond = acceptedInputCount;
            SecondsSinceLastInputBeforeLatestBatch = rawDiscreteBatch.TotalCount > 0 || moveBatch > 0.0
                ? _pendingIdleSecondsBeforeBatch
                : SecondsSinceLastInput;
            TotalActiveSeconds += delta;

            PushAcceptedInputWindow(acceptedInputCount);

            ApThisSecond = acceptedAp;
            ApFinal = acceptedAp;
            ApAccumulator = InputActivityRules.CalculateAccumulator(ApAccumulator, ApFinal, AccumulatorDrainPerSecond, delta);

            EmitSignal(SignalName.ActivityTick, ApThisSecond, ApFinal);
            EmitSignal(SignalName.InputBatchTick, InputEventsThisSecond, ApFinal);
        }

        public void RegisterKeyDown()
        {
            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingKeyDown++;
                TotalKeyDownCount++;
            }
        }

        public void RegisterMouseClick()
        {
            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingMouseClick++;
                TotalMouseClickCount++;
            }
        }

        public void RegisterMouseScroll(int steps)
        {
            if (steps <= 0)
            {
                return;
            }

            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingMouseScroll += steps;
                TotalMouseScrollSteps += steps;
            }
        }

        public void RegisterMouseMove(double distancePx)
        {
            if (distancePx <= 0.0)
            {
                return;
            }

            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingMouseMove += distancePx;
                TotalMouseMoveDistancePx += distancePx;
            }
        }

        public void RegisterJoypadButton()
        {
            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingJoypadButton++;
                TotalJoypadButtonCount++;
            }
        }

        public void RegisterJoypadAxisInput()
        {
            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingJoypadAxis++;
                TotalJoypadAxisInputCount++;
            }
        }

        private void DrainPendingInput(
            out int keyBatch,
            out int clickBatch,
            out int scrollBatch,
            out double moveBatch,
            out int joypadButtonBatch,
            out int joypadAxisBatch)
        {
            lock (_pendingLock)
            {
                keyBatch = _pendingKeyDown;
                clickBatch = _pendingMouseClick;
                scrollBatch = _pendingMouseScroll;
                moveBatch = _pendingMouseMove;
                joypadButtonBatch = _pendingJoypadButton;
                joypadAxisBatch = _pendingJoypadAxis;

                _pendingKeyDown = 0;
                _pendingMouseClick = 0;
                _pendingMouseScroll = 0;
                _pendingMouseMove = 0.0;
                _pendingJoypadButton = 0;
                _pendingJoypadAxis = 0;
                _pendingIdleSecondsBeforeBatch = _hasPendingIdleSnapshot ? _pendingIdleSecondsBeforeBatch : SecondsSinceLastInput;
                _hasPendingIdleSnapshot = false;
            }
        }

        private void MarkInputActivity()
        {
            if (!_hasPendingIdleSnapshot)
            {
                _pendingIdleSecondsBeforeBatch = SecondsSinceLastInput;
                _hasPendingIdleSnapshot = true;
            }

            SecondsSinceLastInput = 0.0;
        }

        private void PushAcceptedInputWindow(int acceptedInputCount)
        {
            if (acceptedInputCount <= 0)
            {
                return;
            }

            _acceptedInputWindow.Enqueue((_runtimeSeconds, acceptedInputCount));
            _acceptedInputCountInWindow += acceptedInputCount;
        }

        private void PruneAcceptedInputWindow()
        {
            double cutoff = _runtimeSeconds - System.Math.Max(1.0, InputWindowSeconds);
            while (_acceptedInputWindow.Count > 0 && _acceptedInputWindow.Peek().time < cutoff)
            {
                var ev = _acceptedInputWindow.Dequeue();
                _acceptedInputCountInWindow -= ev.acceptedInputCount;
            }

            if (_acceptedInputWindow.Count > MaxRollingQueueSize)
            {
                _acceptedInputWindow.Clear();
                _acceptedInputCountInWindow = 0;
            }

            if (_acceptedInputCountInWindow < 0)
            {
                _acceptedInputCountInWindow = 0;
            }
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            lock (_pendingLock)
            {
                return new Godot.Collections.Dictionary<string, Variant>
                {
                    ["total_key_down"] = TotalKeyDownCount,
                    ["total_mouse_click"] = TotalMouseClickCount,
                    ["total_scroll_steps"] = TotalMouseScrollSteps,
                    ["total_move_distance"] = TotalMouseMoveDistancePx,
                    ["total_joypad_button"] = TotalJoypadButtonCount,
                    ["total_joypad_axis"] = TotalJoypadAxisInputCount,
                    ["total_active_seconds"] = TotalActiveSeconds,
                    ["ap_accumulator"] = ApAccumulator
                };
            }
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            lock (_pendingLock)
            {
                TotalKeyDownCount = data.ContainsKey("total_key_down") ? data["total_key_down"].AsInt64() : 0L;
                TotalMouseClickCount = data.ContainsKey("total_mouse_click") ? data["total_mouse_click"].AsInt64() : 0L;
                TotalMouseScrollSteps = data.ContainsKey("total_scroll_steps") ? data["total_scroll_steps"].AsInt64() : 0L;
                TotalMouseMoveDistancePx = data.ContainsKey("total_move_distance") ? data["total_move_distance"].AsDouble() : 0.0;
                TotalJoypadButtonCount = data.ContainsKey("total_joypad_button") ? data["total_joypad_button"].AsInt64() : 0L;
                TotalJoypadAxisInputCount = data.ContainsKey("total_joypad_axis") ? data["total_joypad_axis"].AsInt64() : 0L;
                TotalActiveSeconds = data.ContainsKey("total_active_seconds") ? data["total_active_seconds"].AsDouble() : 0.0;
                ApAccumulator = data.ContainsKey("ap_accumulator") ? data["ap_accumulator"].AsDouble() : 0.0;
            }
        }

        public void ResetCurrentTick()
        {
            lock (_pendingLock)
            {
                _pendingKeyDown = 0;
                _pendingMouseClick = 0;
                _pendingMouseScroll = 0;
                _pendingMouseMove = 0.0;
                _pendingJoypadButton = 0;
                _pendingJoypadAxis = 0;
            }

            KeyDownCount = 0;
            MouseClickCount = 0;
            MouseScrollSteps = 0;
            MouseMoveDistancePx = 0.0;
            JoypadButtonCount = 0;
            JoypadAxisInputCount = 0;
            ApThisSecond = 0.0;
            ApFinal = 0.0;
            InputEventsThisSecond = 0;
            SecondsSinceLastInputBeforeLatestBatch = SecondsSinceLastInput;

            _acceptedInputWindow.Clear();
            _acceptedInputCountInWindow = 0;
        }
    }
}
