using Godot;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Ui
{
    /// <summary>
    /// Lightweight Toast notification queue.
    /// Shows brief messages at top-center of the screen, auto-dismissing after a few seconds.
    /// </summary>
    public partial class ToastController : Control
    {
        private const float ToastDuration = 2.8f;
        private const float FadeOutDuration = 0.4f;
        private const int MaxVisible = 3;
        private const int ToastWidth = 400;
        private const int ToastHeight = 36;
        private const int ToastGap = 6;
        private const int TopMargin = 12;

        private readonly Queue<(string message, Color color)> _pendingQueue = new();
        private readonly List<(PanelContainer panel, float remaining)> _activeToasts = new();

        // Track last craft progress to detect completion edges
        private float _lastAlchemyProgress;
        private float _lastSmithingProgress;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            SubscribeEvents();
        }

        public override void _ExitTree()
        {
            UnsubscribeEvents();
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            // Update active toasts
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                var (panel, remaining) = _activeToasts[i];
                remaining -= dt;
                _activeToasts[i] = (panel, remaining);

                if (remaining <= FadeOutDuration)
                {
                    panel.Modulate = new Color(1, 1, 1, Mathf.Max(0, remaining / FadeOutDuration));
                }

                if (remaining <= 0)
                {
                    panel.QueueFree();
                    _activeToasts.RemoveAt(i);
                    RepositionToasts();
                }
            }

            // Show pending toasts if slots available
            while (_activeToasts.Count < MaxVisible && _pendingQueue.Count > 0)
            {
                var (message, color) = _pendingQueue.Dequeue();
                ShowToast(message, color);
            }
        }

        /// <summary>
        /// Enqueue a toast message for display.
        /// </summary>
        public void Enqueue(string message, Color? color = null)
        {
            _pendingQueue.Enqueue((message, color ?? new Color("4B3622")));
        }

        private void ShowToast(string message, Color textColor)
        {
            var panel = new PanelContainer();
            panel.MouseFilter = MouseFilterEnum.Ignore;

            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color("1E1A14", 0.92f);
            styleBox.BorderColor = new Color("C8A050");
            styleBox.SetBorderWidthAll(1);
            styleBox.SetCornerRadiusAll(6);
            styleBox.ContentMarginLeft = 16;
            styleBox.ContentMarginRight = 16;
            styleBox.ContentMarginTop = 6;
            styleBox.ContentMarginBottom = 6;
            panel.AddThemeStyleboxOverride("panel", styleBox);

            var label = new Label();
            label.Text = message;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.AddThemeColorOverride("font_color", textColor);
            label.AddThemeFontSizeOverride("font_size", 13);
            label.MouseFilter = MouseFilterEnum.Ignore;
            panel.AddChild(label);

            panel.CustomMinimumSize = new Vector2(0, ToastHeight);
            panel.Size = new Vector2(ToastWidth, ToastHeight);

            AddChild(panel);
            _activeToasts.Add((panel, ToastDuration));
            RepositionToasts();
        }

        private void RepositionToasts()
        {
            float viewWidth = GetViewportRect().Size.X;
            float x = (viewWidth - ToastWidth) / 2;
            float y = TopMargin;

            for (int i = 0; i < _activeToasts.Count; i++)
            {
                var (panel, _) = _activeToasts[i];
                panel.Position = new Vector2(x, y);
                y += ToastHeight + ToastGap;
            }
        }

        // ---------- Event subscriptions ----------

        private void SubscribeEvents()
        {
            ServiceLocator? services = ServiceLocator.Instance;
            if (services == null) return;

            services.PlayerProgressState?.Connect(
                PlayerProgressState.SignalName.RealmLevelUp,
                Callable.From<int>(OnRealmLevelUp));

            services.SubsystemMasteryState?.Connect(
                SubsystemMasteryState.SignalName.MasteryChanged,
                Callable.From<string, int>(OnMasteryChanged));

            services.BackpackState?.Connect(
                BackpackState.SignalName.EquipmentInventoryChanged,
                Callable.From(OnEquipmentInventoryChanged));

            if (services.AlchemyState != null)
            {
                services.AlchemyState.AlchemyChanged += OnAlchemyChanged;
            }

            if (services.SmithingState != null)
            {
                services.SmithingState.SmithingChanged += OnSmithingChanged;
            }

            if (services.CultivationRhythmState != null)
            {
                services.CultivationRhythmState.RhythmSummaryReady += OnRhythmSummaryReady;
            }

            if (services.ShopState != null)
            {
                services.ShopState.ShopNoticeReady += OnShopNoticeReady;
            }
        }

        private void UnsubscribeEvents()
        {
            ServiceLocator? services = ServiceLocator.Instance;
            if (services == null) return;

            if (services.PlayerProgressState != null && services.PlayerProgressState.IsConnected(
                    PlayerProgressState.SignalName.RealmLevelUp,
                    Callable.From<int>(OnRealmLevelUp)))
            {
                services.PlayerProgressState.Disconnect(
                    PlayerProgressState.SignalName.RealmLevelUp,
                    Callable.From<int>(OnRealmLevelUp));
            }

            if (services.SubsystemMasteryState != null && services.SubsystemMasteryState.IsConnected(
                    SubsystemMasteryState.SignalName.MasteryChanged,
                    Callable.From<string, int>(OnMasteryChanged)))
            {
                services.SubsystemMasteryState.Disconnect(
                    SubsystemMasteryState.SignalName.MasteryChanged,
                    Callable.From<string, int>(OnMasteryChanged));
            }

            if (services.BackpackState != null && services.BackpackState.IsConnected(
                    BackpackState.SignalName.EquipmentInventoryChanged,
                    Callable.From(OnEquipmentInventoryChanged)))
            {
                services.BackpackState.Disconnect(
                    BackpackState.SignalName.EquipmentInventoryChanged,
                    Callable.From(OnEquipmentInventoryChanged));
            }

            if (services.AlchemyState != null)
            {
                services.AlchemyState.AlchemyChanged -= OnAlchemyChanged;
            }

            if (services.SmithingState != null)
            {
                services.SmithingState.SmithingChanged -= OnSmithingChanged;
            }

            if (services.CultivationRhythmState != null)
            {
                services.CultivationRhythmState.RhythmSummaryReady -= OnRhythmSummaryReady;
            }

            if (services.ShopState != null)
            {
                services.ShopState.ShopNoticeReady -= OnShopNoticeReady;
            }
        }

        // ---------- Event handlers ----------

        private void OnRealmLevelUp(int newRealmLevel)
        {
            Enqueue($"突破成功 → 炼气{newRealmLevel}层", new Color("C8A050"));
        }

        private void OnMasteryChanged(string systemId, int newLevel)
        {
            string name = UiText.MasterySystemName(systemId);
            Enqueue($"领悟成功 → {name} Lv{newLevel}", new Color("C8A050"));
        }

        private void OnEquipmentInventoryChanged()
        {
            Enqueue("获得新装备，请查看背包", new Color("6689B3"));
        }

        private void OnAlchemyChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
        {
            // Detect completion edge: progress reset after reaching required
            if (_lastAlchemyProgress > 0 && _lastAlchemyProgress >= requiredProgress && currentProgress < _lastAlchemyProgress)
            {
                Enqueue("炼丹完成", new Color("8CB870"));
            }

            _lastAlchemyProgress = currentProgress;
        }

        private void OnSmithingChanged(string targetEquipmentId, float currentProgress, float requiredProgress)
        {
            if (_lastSmithingProgress > 0 && _lastSmithingProgress >= requiredProgress && currentProgress < _lastSmithingProgress)
            {
                Enqueue("强化完成", new Color("8CB870"));
            }

            _lastSmithingProgress = currentProgress;
        }

        private void OnRhythmSummaryReady(string title, string rewardSummary, string suggestion, bool requestAttention)
        {
            string message = string.IsNullOrEmpty(rewardSummary) ? title : $"{title}｜{rewardSummary}";
            Enqueue(message, new Color("C8A050"));
            if (!string.IsNullOrEmpty(suggestion))
            {
                Enqueue(suggestion, new Color("D8CBA6"));
            }

            if (requestAttention)
            {
                DisplayServer.WindowRequestAttention();
            }
        }

        private void OnShopNoticeReady(string title, string detail)
        {
            Enqueue(string.IsNullOrEmpty(detail) ? title : $"{title}｜{detail}", new Color("C8A050"));
        }
    }
}
