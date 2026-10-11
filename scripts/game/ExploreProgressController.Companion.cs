using Godot;
using System;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private Button? _workModeButton;
        private TextureRect? _companionTexture;
        private Vector2 _companionOrigin;
        private double _companionSeconds;
        private bool _companionWorkModeActive;
        private Label? _activitySummaryLabel;
        private TextureRect? _compactCompanion;
        private void CreateCompanionControls()
        {
            _companionTexture = GetNodeOrNull<TextureRect>(PlayerSlotTexturePath);
            _companionOrigin = _companionTexture?.Position ?? Vector2.Zero;
            _workModeButton = new Button { Name = "WorkModeButton", Text = "工作模式", Size = new Vector2(110, 28), TooltipText = "收起战斗细节和普通提醒，收益照常累积，成果保留在动态。" };
            _workModeButton.Pressed += () =>
            {
                _actionState?.SetWorkMode(!(_actionState?.WorkMode ?? false));
            };
            _battleInfoLabel.GetParent().AddChild(_workModeButton);
            _workModeButton.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
            _workModeButton.OffsetLeft = -126; _workModeButton.OffsetRight = -16;
            _workModeButton.OffsetTop = -36; _workModeButton.OffsetBottom = -8;
            _activitySummaryLabel = new Label { Name = "ActivitySummary", Position = new Vector2(40, 0), ClipText = true };
            _battleInfoLabel.GetParent().AddChild(_activitySummaryLabel);
            _compactCompanion = new TextureRect { Name = "CompactCompanion", Texture = _companionTexture?.Texture, Position = new Vector2(4, 1), Size = new Vector2(24, 24), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
            _battleInfoLabel.GetParent().AddChild(_compactCompanion);
            _compactCompanion.Size = new Vector2(24, 24);
            if (_playerProgressState != null) _playerProgressState.RealmLevelUp += OnRealmMilestone;
        }
        private void AdvanceCompanionPresentation(double delta)
        {
            _companionSeconds += Math.Max(0, delta);
            if (_companionTexture != null)
                _companionTexture.Position = _companionOrigin + new Vector2(0, (float)Math.Sin(_companionSeconds * 1.2) * 1.5f);
            bool work = _actionState?.WorkMode ?? false;
            if (_workModeButton != null) _workModeButton.Text = work ? "退出工作模式" : "工作模式";
            if (_companionWorkModeActive && !work) { _battleInfoLabel.Show(); _roundInfoLabel.Show(); }
            _companionWorkModeActive = work;
            if (work) { _battleInfoLabel.Hide(); _roundInfoLabel.Hide(); }
            var mainBar = GetNode<MainBarLayoutController>("../MainBarWindow");
            bool expanded = _inBattle && !work && !GetNode<Control>("../SubmenuBookWindow").Visible;
            mainBar.SetBattleExpanded(expanded);
            _battleTrackVisualizer.SetStageVisible(expanded);
            if (expanded)
            {
                _battleTrackVisualizer.ApplyGameState(_logic, _levelConfigLoader);
                var track = _battleInfoLabel.GetParent<Control>();
                _battleInfoLabel.Position = new Vector2(132, 6); _battleInfoLabel.Size = new Vector2(track.Size.X - 144, 24); _battleInfoLabel.ClipText = true;
                _roundInfoLabel.Position = new Vector2(132, 32); _roundInfoLabel.Size = new Vector2(track.Size.X - 144, 24); _roundInfoLabel.ClipText = true;
                _battleInfoLabel.Show(); _roundInfoLabel.Show();
            }
            else { _battleInfoLabel.Hide(); _roundInfoLabel.Hide(); }
            if (_activitySummaryLabel != null)
            {
                _activitySummaryLabel.Visible = !expanded;
                _activitySummaryLabel.Size = new Vector2(Math.Max(100, _battleInfoLabel.GetParent<Control>().Size.X - 48), 26);
                _activitySummaryLabel.Text = work ? "静心工作 · 收益照常累积" : HasDungeonCapability()
                    ? _inBattle ? $"正在与 {UiText.LocalizeGameNames(_battleMonsterName)} 交战" : $"副本 · 探索 {_exploreProgress:0}%"
                    : $"{GetPausedModeLabel()} · {GetPausedModeRoundLabel()}";
            }
            if (_compactCompanion != null) _compactCompanion.Visible = !expanded;
            if (_compactCompanion != null) _compactCompanion.Position = new Vector2(4, 1 + (float)Math.Sin(_companionSeconds * 1.2) * .5f);
            if (expanded && _companionTexture != null) _companionTexture.Position = _companionOrigin + new Vector2(0, (float)Math.Sin(_companionSeconds * 1.2) * 1.5f);
            if (_bossChallengeButton != null)
            {
                _bossChallengeButton.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
                _bossChallengeButton.Position = new Vector2(Math.Max(40, _battleInfoLabel.GetParent<Control>().Size.X - 116), 0);
                _bossChallengeButton.Size = new Vector2(110, 26);
                _bossChallengeButton.Visible = CanStartBossChallenge && !GetNode<Control>("../SubmenuBookWindow").Visible;
                if (_bossChallengeButton.Visible && _activitySummaryLabel != null) _activitySummaryLabel.Size -= new Vector2(120, 0);
            }
        }
        private void OnRealmMilestone(int level)
        {
            if (_companionTexture == null || _actionState?.WorkMode == true) return;
            Tween ritual = CreateTween();
            ritual.TweenProperty(_companionTexture, "modulate", new Color("FFD782"), .25);
            ritual.TweenProperty(_companionTexture, "modulate", Colors.White, 1.0);
        }
    }
}
