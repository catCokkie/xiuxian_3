using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Event-driven input aggregator with anti-abuse guards:
    /// 1) rolling-window frequency decay
    /// 2) per-minute soft cap
    /// </summary>
    public partial class InputActivityState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void ActivityTickEventHandler(double apThisBatch, double apFinal);

        [Signal]
        public delegate void InputBatchTickEventHandler(int inputEventsThisBatch, double apFinal);

        // Latest batch counters (updated every frame when pending input exists)
        public int KeyDownCount { get; private set; }
        public int MouseClickCount { get; private set; }
        public int MouseScrollSteps { get; private set; }
        public double MouseMoveDistancePx { get; private set; }
        public int JoypadButtonCount { get; private set; }
        public int JoypadAxisInputCount { get; private set; }

        // Lifetime counters
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

        [Export] public double ApBaseline { get; set; } = GameBalanceConstants.InputDecay.ApBaseline;
        [Export] public double KeyDownWeight { get; set; } = 1.0;
        [Export] public double MouseClickWeight { get; set; } = 1.2;
        [Export] public double ScrollStepWeight { get; set; } = 0.4;
        [Export] public double MovePxDivider { get; set; } = 600.0;
        [Export] public double JoypadButtonWeight { get; set; } = 1.0;
        [Export] public double JoypadAxisWeight { get; set; } = 0.8;

        [Export] public double DecayThreshold { get; set; } = GameBalanceConstants.InputDecay.DecayThreshold;
        [Export] public double DecayRate { get; set; } = GameBalanceConstants.InputDecay.DecayRate;
        [Export] public double MinDecayMultiplier { get; set; } = GameBalanceConstants.InputDecay.MinDecayMultiplier;

        [Export] public double RollingWindowSeconds { get; set; } = 10.0;
        [Export] public double SoftCapPerMinute { get; set; } = 420.0;
        [Export] public double MinCapMultiplier { get; set; } = 0.20;
        [Export] public double AccumulatorDrainPerSecond { get; set; } = 0.6;

        private readonly object _pendingLock = new();
        private readonly Queue<(double time, double rawAp)> _rollingEvents = new();

        private double _pendingRawAp;
        private int _pendingInputEvents;
        private int _pendingKeyDown;
        private int _pendingMouseClick;
        private int _pendingMouseScroll;
        private double _pendingMouseMove;
        private int _pendingJoypadButton;
        private int _pendingJoypadAxis;

        private double _rollingRawApSum;
        private double _runtimeSeconds;
        private double _minuteWindowStartSeconds;
        private double _apFinalThisMinute;
        private double _pendingIdleSecondsBeforeBatch;
        private bool _hasPendingIdleSnapshot;

        public override void _Process(double delta)
        {
            _runtimeSeconds += delta;
            SecondsSinceLastInput += delta;

            if (_runtimeSeconds - _minuteWindowStartSeconds >= 60.0)
            {
                _minuteWindowStartSeconds = _runtimeSeconds;
                _apFinalThisMinute = 0.0;
            }

            DrainPendingInput(
                out double apRawBatch,
                out int inputBatch,
                out int keyBatch,
                out int clickBatch,
                out int scrollBatch,
                out double moveBatch,
                out int joypadButtonBatch,
                out int joypadAxisBatch);
            if (apRawBatch <= 0.0 && inputBatch <= 0)
            {
                return;
            }

            KeyDownCount = keyBatch;
            MouseClickCount = clickBatch;
            MouseScrollSteps = scrollBatch;
            MouseMoveDistancePx = moveBatch;
            JoypadButtonCount = joypadButtonBatch;
            JoypadAxisInputCount = joypadAxisBatch;
            InputEventsThisSecond = inputBatch;
            SecondsSinceLastInputBeforeLatestBatch = inputBatch > 0 ? _pendingIdleSecondsBeforeBatch : SecondsSinceLastInput;
            TotalActiveSeconds += delta;

            PushRollingWindow(apRawBatch);
            PruneRollingWindow();

            double apPerSecondEquivalent = RollingWindowSeconds > 0.0 ? _rollingRawApSum / RollingWindowSeconds : _rollingRawApSum;
            double decayMultiplier = InputActivityRules.CalculateDecayMultiplier(
                apPerSecondEquivalent,
                ApBaseline,
                DecayThreshold,
                DecayRate,
                MinDecayMultiplier);
            double capMultiplier = InputActivityRules.CalculateCapMultiplier(_apFinalThisMinute, SoftCapPerMinute, MinCapMultiplier);

            ApThisSecond = apRawBatch;
            ApFinal = apRawBatch * decayMultiplier * capMultiplier;
            _apFinalThisMinute += ApFinal;

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
                _pendingInputEvents++;
                _pendingRawAp += KeyDownWeight;
                TotalKeyDownCount++;
            }
        }

        public void RegisterMouseClick()
        {
            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingMouseClick++;
                _pendingInputEvents++;
                _pendingRawAp += MouseClickWeight;
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
                _pendingInputEvents += steps;
                _pendingRawAp += steps * ScrollStepWeight;
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
                _pendingRawAp += distancePx / MovePxDivider;
                TotalMouseMoveDistancePx += distancePx;
            }
        }

        public void RegisterJoypadButton()
        {
            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingJoypadButton++;
                _pendingInputEvents++;
                _pendingRawAp += JoypadButtonWeight;
                TotalJoypadButtonCount++;
            }
        }

        public void RegisterJoypadAxisInput()
        {
            lock (_pendingLock)
            {
                MarkInputActivity();
                _pendingJoypadAxis++;
                _pendingInputEvents++;
                _pendingRawAp += JoypadAxisWeight;
                TotalJoypadAxisInputCount++;
            }
        }

        private void DrainPendingInput(
            out double apRawBatch,
            out int inputBatch,
            out int keyBatch,
            out int clickBatch,
            out int scrollBatch,
            out double moveBatch,
            out int joypadButtonBatch,
            out int joypadAxisBatch)
        {
            lock (_pendingLock)
            {
                apRawBatch = _pendingRawAp;
                inputBatch = _pendingInputEvents;
                keyBatch = _pendingKeyDown;
                clickBatch = _pendingMouseClick;
                scrollBatch = _pendingMouseScroll;
                moveBatch = _pendingMouseMove;
                joypadButtonBatch = _pendingJoypadButton;
                joypadAxisBatch = _pendingJoypadAxis;

                _pendingRawAp = 0.0;
                _pendingInputEvents = 0;
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

        private void PushRollingWindow(double apRawBatch)
        {
            _rollingEvents.Enqueue((_runtimeSeconds, apRawBatch));
            _rollingRawApSum += apRawBatch;
        }

        private const int MaxRollingQueueSize = 10000;

        private void PruneRollingWindow()
        {
            double cutoff = _runtimeSeconds - Math.Max(1.0, RollingWindowSeconds);
            while (_rollingEvents.Count > 0 && _rollingEvents.Peek().time < cutoff)
            {
                var ev = _rollingEvents.Dequeue();
                _rollingRawApSum -= ev.rawAp;
            }

            if (_rollingEvents.Count > MaxRollingQueueSize)
            {
                _rollingEvents.Clear();
                _rollingRawApSum = 0.0;
            }

            if (_rollingRawApSum < 0.0)
            {
                _rollingRawApSum = 0.0;
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
                _pendingRawAp = 0.0;
                _pendingInputEvents = 0;
                _pendingKeyDown = 0;
                _pendingMouseClick = 0;
                _pendingMouseScroll = 0;
                _pendingMouseMove = 0.0;
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

            _rollingEvents.Clear();
            _rollingRawApSum = 0.0;
        }
    }
}
