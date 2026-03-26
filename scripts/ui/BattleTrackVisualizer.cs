using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.UI
{
    public sealed partial class BattleTrackVisualizer : Node
    {
        public readonly record struct MonsterAssignment(string MonsterId, int MoveThreshold);
        public readonly record struct FrontMonsterInfo(int Index, string MonsterId, float X);

        public sealed class RuntimeState
        {
            public int QueueMoveInputPending { get; init; }
            public List<MarkerState> MarkerStates { get; } = new();
        }

        public sealed class MarkerState
        {
            public float X { get; init; }
            public float Y { get; init; }
            public string MonsterId { get; init; } = string.Empty;
            public int MovePending { get; init; }
            public int MoveThreshold { get; init; }
        }

        private Label _playerMarker = null!;
        private Label _playerHpLabel = null!;
        private Label _enemyHpLabel = null!;
        private TextureRect? _playerSlotTexture;
        private Label? _playerSlotLabel;
        private TextureRect? _enemySlotTexture;
        private Label? _enemySlotLabel;

        private readonly List<Label> _monsterMarkers = new();
        private readonly List<string> _monsterMarkerIds = new();
        private readonly List<TextureRect> _monsterSlots = new();
        private readonly List<int> _monsterMoveInputPending = new();
        private readonly List<int> _monsterMoveInputThreshold = new();

        private int _defaultInputsPerMoveFrame = 4;
        private float _monsterMovePxPerFrame = GameBalanceConstants.Explore.MonsterMovePxPerFrame;
        private float _monsterRespawnSpacing = GameBalanceConstants.Explore.MonsterRespawnSpacing;
        private string _activeEnemyVisualMonsterId = string.Empty;
        private string _enemySlotAnimType = "none";
        private float _enemySlotAnimSpeed;
        private float _enemySlotAnimAmplitude;
        private Vector2 _enemySlotBasePosition;
        private Texture2D? _enemySlotDefaultTexture;
        private double _enemyVisualTime;

        public int QueueMoveInputPending { get; private set; }
        public bool HasMarkers => _monsterMarkers.Count > 0;

        public void Bind(
            Node host,
            NodePath playerMarkerPath,
            NodePath playerHpLabelPath,
            NodePath enemyHpLabelPath,
            NodePath playerSlotTexturePath,
            NodePath playerSlotLabelPath,
            NodePath enemySlotTexturePath,
            NodePath enemySlotLabelPath,
            int defaultInputsPerMoveFrame,
            float monsterMovePxPerFrame,
            float monsterRespawnSpacing)
        {
            _playerMarker = host.GetNode<Label>(playerMarkerPath);
            _playerHpLabel = host.GetNode<Label>(playerHpLabelPath);
            _enemyHpLabel = host.GetNode<Label>(enemyHpLabelPath);
            _playerSlotTexture = host.GetNodeOrNull<TextureRect>(playerSlotTexturePath);
            _playerSlotLabel = host.GetNodeOrNull<Label>(playerSlotLabelPath);
            _enemySlotTexture = host.GetNodeOrNull<TextureRect>(enemySlotTexturePath);
            _enemySlotLabel = host.GetNodeOrNull<Label>(enemySlotLabelPath);
            _defaultInputsPerMoveFrame = Math.Max(1, defaultInputsPerMoveFrame);
            _monsterMovePxPerFrame = monsterMovePxPerFrame;
            _monsterRespawnSpacing = monsterRespawnSpacing;

            if (_enemySlotTexture != null)
            {
                _enemySlotDefaultTexture = _enemySlotTexture.Texture;
                _enemySlotTexture.PivotOffset = _enemySlotTexture.Size * 0.5f;
            }

            CacheMonsterMarkers(host, playerMarkerPath);
            CacheMonsterSlots(host, playerMarkerPath);
        }

        public FrontMonsterInfo GetFrontMonsterInfo()
        {
            int index = -1;
            float bestX = float.MaxValue;
            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                float x = _monsterMarkers[i].Position.X;
                if (x >= _playerMarker.Position.X + 50.0f && x < bestX)
                {
                    bestX = x;
                    index = i;
                }
            }

            string monsterId = index >= 0 && index < _monsterMarkerIds.Count ? _monsterMarkerIds[index] : string.Empty;
            return new FrontMonsterInfo(index, monsterId, index >= 0 ? _monsterMarkers[index].Position.X : float.MaxValue);
        }

        public int ResolveBattleMonsterIndex(int battleMonsterIndex)
        {
            if (battleMonsterIndex >= 0 && battleMonsterIndex < _monsterMarkers.Count)
            {
                return battleMonsterIndex;
            }

            return GetFrontMonsterInfo().Index;
        }

        public string GetMonsterId(int markerIndex)
        {
            return markerIndex >= 0 && markerIndex < _monsterMarkerIds.Count
                ? _monsterMarkerIds[markerIndex]
                : string.Empty;
        }

        public int MoveMonsterQueueByInputs(int inputEvents, Func<MonsterAssignment> assignmentFactory)
        {
            if (inputEvents <= 0)
            {
                return 0;
            }

            int movedFrames = 0;
            QueueMoveInputPending += inputEvents;
            int baseThreshold = Math.Max(1, _defaultInputsPerMoveFrame);
            int queueFrames = QueueMoveInputPending / baseThreshold;
            if (queueFrames > 0)
            {
                QueueMoveInputPending -= queueFrames * baseThreshold;
                float queueShift = queueFrames * _monsterMovePxPerFrame;
                for (int i = 0; i < _monsterMarkers.Count; i++)
                {
                    Label marker = _monsterMarkers[i];
                    marker.Position = new Vector2(marker.Position.X - queueShift, marker.Position.Y);
                }
                movedFrames += queueFrames;
            }

            FrontMonsterInfo front = GetFrontMonsterInfo();
            if (front.Index >= 0 && front.Index < _monsterMarkers.Count)
            {
                int threshold = front.Index < _monsterMoveInputThreshold.Count
                    ? Math.Max(1, _monsterMoveInputThreshold[front.Index])
                    : baseThreshold;
                if (front.Index < _monsterMoveInputPending.Count)
                {
                    _monsterMoveInputPending[front.Index] += inputEvents;
                }

                int bonusFrames = front.Index < _monsterMoveInputPending.Count
                    ? _monsterMoveInputPending[front.Index] / threshold
                    : inputEvents / threshold;
                if (bonusFrames > 0)
                {
                    if (front.Index < _monsterMoveInputPending.Count)
                    {
                        _monsterMoveInputPending[front.Index] -= bonusFrames * threshold;
                    }

                    Label marker = _monsterMarkers[front.Index];
                    float bonusShift = bonusFrames * _monsterMovePxPerFrame;
                    marker.Position = new Vector2(marker.Position.X - bonusShift, marker.Position.Y);
                    movedFrames += bonusFrames;
                }
            }

            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                Label marker = _monsterMarkers[i];
                if (marker.Position.X >= 120.0f)
                {
                    continue;
                }

                float rightMostX = Mathf.Max(GetRightMostMonsterX(), marker.Position.X);
                marker.Position = new Vector2(rightMostX + _monsterRespawnSpacing, marker.Position.Y);
                AssignMonsterToMarker(i, assignmentFactory());
            }

            return movedFrames;
        }

        public void ResetTrackVisual(Func<MonsterAssignment> assignmentFactory)
        {
            ResetMonsterMoveState();
            _activeEnemyVisualMonsterId = string.Empty;
            _enemyVisualTime = 0.0;
            float startX = 540.0f;
            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                _monsterMarkers[i].Visible = true;
                _monsterMarkers[i].Modulate = Colors.White;
                _monsterMarkers[i].Position = new Vector2(startX + i * _monsterRespawnSpacing, _monsterMarkers[i].Position.Y);
                AssignMonsterToMarker(i, assignmentFactory());
            }
        }

        public void RecycleMonsterAt(int markerIndex, Func<MonsterAssignment> assignmentFactory)
        {
            if (markerIndex < 0 || markerIndex >= _monsterMarkers.Count)
            {
                return;
            }

            Label marker = _monsterMarkers[markerIndex];
            marker.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.45f);
            marker.Position = new Vector2(GetRightMostMonsterX() + _monsterRespawnSpacing, marker.Position.Y);
            marker.Modulate = Colors.White;
            AssignMonsterToMarker(markerIndex, assignmentFactory());
        }

        public void ApplyGameState(ExploreGameLogic logic, LevelConfigLoader? levelConfigLoader)
        {
            UpdateHpLabels(logic);
            RefreshActorSlots(logic, levelConfigLoader);
        }

        public void AdvanceEnemyVisual(double delta)
        {
            if (_enemySlotTexture == null || !_enemySlotTexture.Visible)
            {
                return;
            }

            _enemyVisualTime += delta;
            float t = (float)_enemyVisualTime;
            _enemySlotTexture.Position = _enemySlotBasePosition;
            _enemySlotTexture.Scale = Vector2.One;

            switch (_enemySlotAnimType)
            {
                case "hover":
                    _enemySlotTexture.Position += new Vector2(0.0f, Mathf.Sin(t * _enemySlotAnimSpeed) * _enemySlotAnimAmplitude);
                    break;
                case "pulse":
                    float factor = 1.0f + Mathf.Sin(t * _enemySlotAnimSpeed) * _enemySlotAnimAmplitude;
                    _enemySlotTexture.Scale = new Vector2(factor, factor);
                    break;
            }
        }

        public string BuildFrontMoveStatus()
        {
            FrontMonsterInfo front = GetFrontMonsterInfo();
            if (front.Index < 0 || front.Index >= _monsterMoveInputThreshold.Count || front.Index >= _monsterMoveInputPending.Count)
            {
                return "move idle";
            }

            int threshold = Math.Max(1, _monsterMoveInputThreshold[front.Index]);
            int pending = Math.Clamp(_monsterMoveInputPending[front.Index], 0, threshold);
            int remaining = Math.Max(0, threshold - pending);
            return $"move remain {remaining} ({pending}/{threshold})";
        }

        public string BuildMoveDebugStatus()
        {
            FrontMonsterInfo front = GetFrontMonsterInfo();
            if (front.Index < 0 || front.Index >= _monsterMoveInputThreshold.Count || front.Index >= _monsterMoveInputPending.Count)
            {
                return ExploreProgressDebugRules.BuildMoveDebugStatus(-1, 0, 0, "unknown");
            }

            return ExploreProgressDebugRules.BuildMoveDebugStatus(
                front.Index,
                Math.Max(1, _monsterMoveInputThreshold[front.Index]),
                _monsterMoveInputPending[front.Index],
                front.MonsterId);
        }

        public string BuildWaveDebugStatus(LevelConfigLoader? levelConfigLoader, bool inBattle, int battleMonsterIndex)
        {
            int frontIndex = inBattle ? battleMonsterIndex : GetFrontMonsterInfo().Index;
            string frontMonsterId = frontIndex >= 0 && frontIndex < _monsterMarkerIds.Count
                ? _monsterMarkerIds[frontIndex]
                : "none";

            if (levelConfigLoader == null)
            {
                return $"调试-副本：loader 不可用 | 当前前排#{frontIndex + 1} [{frontMonsterId}]";
            }

            if (levelConfigLoader.TryGetActiveWaveProgress(out int nextIndex, out int waveCount, out string nextMonsterId))
            {
                return $"调试-副本：波次 {nextIndex}/{waveCount}，next=[{nextMonsterId}] | 当前前排#{frontIndex + 1} [{frontMonsterId}]";
            }

            return $"调试-副本：未配置 monster_wave（使用 spawn_table） | 当前前排#{frontIndex + 1} [{frontMonsterId}]";
        }

        public RuntimeState CaptureRuntimeState()
        {
            var state = new RuntimeState
            {
                QueueMoveInputPending = QueueMoveInputPending
            };
            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                Label marker = _monsterMarkers[i];
                state.MarkerStates.Add(new MarkerState
                {
                    X = marker.Position.X,
                    Y = marker.Position.Y,
                    MonsterId = i < _monsterMarkerIds.Count ? _monsterMarkerIds[i] : string.Empty,
                    MovePending = i < _monsterMoveInputPending.Count ? _monsterMoveInputPending[i] : 0,
                    MoveThreshold = i < _monsterMoveInputThreshold.Count ? _monsterMoveInputThreshold[i] : _defaultInputsPerMoveFrame,
                });
            }

            return state;
        }

        public void RestoreRuntimeState(RuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            QueueMoveInputPending = Math.Max(0, state.QueueMoveInputPending);
            int count = Math.Min(state.MarkerStates.Count, _monsterMarkers.Count);
            for (int i = 0; i < count; i++)
            {
                MarkerState markerState = state.MarkerStates[i];
                Label marker = _monsterMarkers[i];
                marker.Position = new Vector2(markerState.X, markerState.Y);
                if (i < _monsterMarkerIds.Count)
                {
                    _monsterMarkerIds[i] = markerState.MonsterId ?? string.Empty;
                }
                if (i < _monsterMoveInputPending.Count)
                {
                    _monsterMoveInputPending[i] = Math.Max(0, markerState.MovePending);
                }
                if (i < _monsterMoveInputThreshold.Count)
                {
                    _monsterMoveInputThreshold[i] = Math.Max(1, markerState.MoveThreshold);
                }

                ApplyMarkerVisual(marker, _monsterMarkerIds[i]);
            }
        }

        private void CacheMonsterMarkers(Node host, NodePath playerMarkerPath)
        {
            _monsterMarkers.Clear();
            _monsterMarkerIds.Clear();
            _monsterMoveInputPending.Clear();
            _monsterMoveInputThreshold.Clear();
            for (int i = 1; i <= 8; i++)
            {
                NodePath markerPath = $"{playerMarkerPath.GetConcatenatedNames().Replace("PlayerMarker", $"MonsterMarker{i:00}")}";
                Label marker = host.GetNodeOrNull<Label>(markerPath);
                if (marker != null)
                {
                    _monsterMarkers.Add(marker);
                    _monsterMarkerIds.Add(string.Empty);
                    _monsterMoveInputPending.Add(0);
                    _monsterMoveInputThreshold.Add(_defaultInputsPerMoveFrame);
                }
            }
        }

        private void CacheMonsterSlots(Node host, NodePath playerMarkerPath)
        {
            _monsterSlots.Clear();
            for (int i = 1; i <= 8; i++)
            {
                NodePath slotPath = $"{playerMarkerPath.GetConcatenatedNames().Replace("PlayerMarker", $"MonsterSlot{i:00}")}";
                TextureRect slot = host.GetNodeOrNull<TextureRect>(slotPath);
                if (slot != null)
                {
                    _monsterSlots.Add(slot);
                }
            }
        }

        private void UpdateHpLabels(ExploreGameLogic logic)
        {
            _playerHpLabel.Text = $"HP {logic.PlayerHp}/{logic.PlayerMaxHp}";
            _playerHpLabel.Position = new Vector2(_playerMarker.Position.X - 20.0f, _playerMarker.Position.Y + 22.0f);

            if (logic.InBattle && logic.BattleMonsterIndex >= 0 && logic.BattleMonsterIndex < _monsterMarkers.Count)
            {
                Label target = _monsterMarkers[logic.BattleMonsterIndex];
                _enemyHpLabel.Visible = true;
                _enemyHpLabel.Text = $"HP {logic.EnemyHp}/{logic.EnemyMaxHp}";
                _enemyHpLabel.Position = new Vector2(target.Position.X - 24.0f, target.Position.Y + 22.0f);
            }
            else
            {
                _enemyHpLabel.Visible = false;
            }
        }

        private void RefreshActorSlots(ExploreGameLogic logic, LevelConfigLoader? levelConfigLoader)
        {
            if (_playerSlotTexture != null)
            {
                _playerSlotTexture.Position = new Vector2(_playerMarker.Position.X - 16.0f, _playerMarker.Position.Y - 26.0f);
            }
            if (_playerSlotLabel != null)
            {
                _playerSlotLabel.Text = "主角";
                _playerSlotLabel.Position = new Vector2(_playerMarker.Position.X - 12.0f, _playerMarker.Position.Y - 24.0f);
            }

            if (_enemySlotTexture == null || _enemySlotLabel == null)
            {
                return;
            }

            int focusIndex = logic.InBattle ? logic.BattleMonsterIndex : GetFrontMonsterInfo().Index;
            RefreshMonsterSlots(focusIndex);
            if (focusIndex < 0 || focusIndex >= _monsterMarkers.Count)
            {
                _enemySlotTexture.Visible = false;
                _enemySlotLabel.Visible = false;
                _activeEnemyVisualMonsterId = string.Empty;
                return;
            }

            Label focus = _monsterMarkers[focusIndex];
            _enemySlotTexture.Visible = true;
            _enemySlotLabel.Visible = true;
            _enemySlotBasePosition = new Vector2(focus.Position.X - 16.0f, focus.Position.Y - 26.0f);
            _enemySlotTexture.Position = _enemySlotBasePosition;
            _enemySlotLabel.Position = new Vector2(focus.Position.X - 12.0f, focus.Position.Y - 24.0f);
            _enemySlotLabel.Text = logic.InBattle ? logic.BattleMonsterName : "敌人";

            string focusMonsterId = logic.InBattle ? logic.BattleMonsterId : _monsterMarkerIds[focusIndex];
            ApplyEnemyVisualConfig(focusMonsterId, levelConfigLoader);
        }

        private void RefreshMonsterSlots(int focusIndex)
        {
            if (_monsterSlots.Count == 0)
            {
                return;
            }

            int count = Math.Min(_monsterSlots.Count, _monsterMarkers.Count);
            for (int i = 0; i < count; i++)
            {
                TextureRect slot = _monsterSlots[i];
                Label marker = _monsterMarkers[i];
                slot.Position = new Vector2(marker.Position.X - 16.0f, marker.Position.Y - 26.0f);
                slot.Visible = i != focusIndex;
                slot.Modulate = GetMarkerTint(_monsterMarkerIds[i]);
            }
        }

        private void ApplyEnemyVisualConfig(string monsterId, LevelConfigLoader? levelConfigLoader)
        {
            if (_enemySlotTexture == null)
            {
                return;
            }

            if (_activeEnemyVisualMonsterId == monsterId)
            {
                return;
            }

            _activeEnemyVisualMonsterId = monsterId;
            _enemyVisualTime = 0.0;
            _enemySlotAnimType = "none";
            _enemySlotAnimSpeed = 0.0f;
            _enemySlotAnimAmplitude = 0.0f;
            _enemySlotTexture.Scale = Vector2.One;
            _enemySlotTexture.Modulate = Colors.White;
            _enemySlotTexture.Texture = _enemySlotDefaultTexture;

            if (string.IsNullOrEmpty(monsterId) || levelConfigLoader == null)
            {
                return;
            }

            if (!levelConfigLoader.TryGetMonsterVisualConfig(
                monsterId,
                out string portraitPath,
                out string animationType,
                out double animSpeed,
                out double animAmplitude,
                out Color tint))
            {
                return;
            }

            if (!string.IsNullOrEmpty(portraitPath))
            {
                Texture2D? loaded = GD.Load<Texture2D>(portraitPath);
                if (loaded != null)
                {
                    _enemySlotTexture.Texture = loaded;
                }
            }

            _enemySlotTexture.Modulate = tint;
            _enemySlotAnimType = animationType.ToLowerInvariant();
            _enemySlotAnimSpeed = Mathf.Max(0.0f, (float)animSpeed);
            _enemySlotAnimAmplitude = Mathf.Max(0.0f, (float)animAmplitude);
        }

        private void AssignMonsterToMarker(int markerIndex, MonsterAssignment assignment)
        {
            if (markerIndex < 0 || markerIndex >= _monsterMarkers.Count || markerIndex >= _monsterMarkerIds.Count)
            {
                return;
            }

            _monsterMarkerIds[markerIndex] = assignment.MonsterId ?? string.Empty;
            if (markerIndex < _monsterMoveInputThreshold.Count)
            {
                _monsterMoveInputThreshold[markerIndex] = Math.Max(1, assignment.MoveThreshold);
            }
            if (markerIndex < _monsterMoveInputPending.Count)
            {
                _monsterMoveInputPending[markerIndex] = 0;
            }

            ApplyMarkerVisual(_monsterMarkers[markerIndex], _monsterMarkerIds[markerIndex]);
        }

        private void ResetMonsterMoveState()
        {
            QueueMoveInputPending = 0;
            for (int i = 0; i < _monsterMoveInputPending.Count; i++)
            {
                _monsterMoveInputPending[i] = 0;
            }
        }

        private float GetRightMostMonsterX()
        {
            float maxX = 0.0f;
            foreach (Label marker in _monsterMarkers)
            {
                maxX = Mathf.Max(maxX, marker.Position.X);
            }
            return maxX;
        }

        private static void ApplyMarkerVisual(Label marker, string monsterId)
        {
            marker.Modulate = GetMarkerTint(monsterId);
            marker.Text = monsterId switch
            {
                "monster_slime_moss" => "SL",
                "monster_bat_shadow" => "BT",
                "monster_spider_cave" => "SP",
                _ => "MO",
            };
        }

        private static Color GetMarkerTint(string monsterId)
        {
            return monsterId switch
            {
                "monster_slime_moss" => new Color(0.66f, 0.92f, 0.52f, 1.0f),
                "monster_bat_shadow" => new Color(0.75f, 0.75f, 0.92f, 1.0f),
                "monster_spider_cave" => new Color(0.95f, 0.56f, 0.56f, 1.0f),
                _ => new Color(0.9f, 0.9f, 0.9f, 0.92f),
            };
        }
    }
}
