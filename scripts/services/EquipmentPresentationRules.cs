using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentPresentationRules
    {
        public static string BuildEquipmentPageText(
            CharacterStatBlock baseStats,
            CharacterStatBlock finalStats,
            IReadOnlyList<EquipmentStatProfile> equippedProfiles,
            IReadOnlyList<EquipmentInstanceData> backpackInstances,
            IReadOnlyList<EquipmentStatProfile> legacyBackpackProfiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine(UiText.LeftTabEquipment);
            sb.AppendLine($"当前已穿戴 {equippedProfiles.Count} 件");

            int hpDiff = finalStats.MaxHp - baseStats.MaxHp;
            int atkDiff = finalStats.Attack - baseStats.Attack;
            int defDiff = finalStats.Defense - baseStats.Defense;
            sb.AppendLine($"气血(HP)　基础 {baseStats.MaxHp}　装备后 {finalStats.MaxHp} [color=#6aaf6a]+{hpDiff}[/color]");
            sb.AppendLine($"攻击　　　基础 {baseStats.Attack}　装备后 {finalStats.Attack} [color=#6aaf6a]+{atkDiff}[/color]");
            sb.AppendLine($"防御　　　基础 {baseStats.Defense}　装备后 {finalStats.Defense} [color=#6aaf6a]+{defDiff}[/color]");

            for (int i = 0; i < equippedProfiles.Count; i++)
            {
                EquipmentStatProfile profile = equippedProfiles[i];
                string slotIcon = GetSlotIcon(profile.Slot);
                string rarityColor = GetRarityColor(profile.Modifier);
                sb.AppendLine();
                sb.AppendLine($"{slotIcon} [color={rarityColor}]{profile.DisplayName}[/color]");
                sb.AppendLine($"　主属性：{BuildModifierSummary(profile.Modifier)}");
            }

            sb.AppendLine();
            sb.AppendLine($"背包装备 {backpackInstances.Count + legacyBackpackProfiles.Count} 件");

            for (int i = 0; i < backpackInstances.Count; i++)
            {
                EquipmentInstanceData instance = backpackInstances[i];
                sb.AppendLine($"- [{BuildSlotLabel(instance.Slot)}] {instance.DisplayName} | {BuildRarityLabel(instance.RarityTier)} | {BuildSourceLabel(instance.SourceStage)}");
                sb.AppendLine($"  对比当前{BuildSlotLabel(instance.Slot)}：{BuildComparisonHint(instance, equippedProfiles)}");
                sb.AppendLine($"  主属性：{BuildSingleStatLine(instance.MainStatKey, instance.MainStatValue)}");
                sb.AppendLine($"  副属性：{BuildSubStatSummary(instance.SubStats)}");
            }

            for (int i = 0; i < legacyBackpackProfiles.Count; i++)
            {
                EquipmentStatProfile profile = legacyBackpackProfiles[i];
                sb.AppendLine($"- [{BuildSlotLabel(profile.Slot)}] {profile.DisplayName} | 旧版装备");
                sb.AppendLine($"  属性：{BuildModifierSummary(profile.Modifier)}");
            }

            return sb.ToString().TrimEnd();
        }

        public static string BuildRarityLabel(EquipmentRarityTier rarity)
        {
            return UiText.EquipmentRarityLabel(rarity);
        }

        public static string BuildSourceLabel(EquipmentSourceStage sourceStage)
        {
            return UiText.EquipmentSourceLabel(sourceStage);
        }

        public static string BuildSubStatSummary(IReadOnlyList<EquipmentSubStatData> subStats)
        {
            if (subStats == null || subStats.Count == 0)
            {
                return "无";
            }

            List<string> parts = new();
            for (int i = 0; i < subStats.Count; i++)
            {
                parts.Add(BuildSingleStatLine(subStats[i].Stat, subStats[i].Value));
            }

            return string.Join(" | ", parts);
        }

        public static string BuildComparisonHint(EquipmentInstanceData instance, IReadOnlyList<EquipmentStatProfile> equippedProfiles)
        {
            bool foundEquipped = false;
            EquipmentStatProfile equipped = default;
            for (int i = 0; i < equippedProfiles.Count; i++)
            {
                if (equippedProfiles[i].Slot == instance.Slot)
                {
                    equipped = equippedProfiles[i];
                    foundEquipped = true;
                    break;
                }
            }

            if (!foundEquipped)
            {
                return "可直接穿戴";
            }

            EquipmentStatProfile candidate = EquipmentInstanceRules.ToStatProfile(instance);
            double currentScore = ScoreModifier(equipped.Modifier);
            double candidateScore = ScoreModifier(candidate.Modifier);
            double delta = candidateScore - currentScore;
            string breakdown = BuildModifierDeltaBreakdown(candidate.Modifier, equipped.Modifier);
            if (delta > 0.25)
            {
                return string.IsNullOrEmpty(breakdown)
                    ? $"更强，+{delta:0.##}"
                    : $"更强，+{delta:0.##}（{breakdown}）";
            }

            if (delta < -0.25)
            {
                return string.IsNullOrEmpty(breakdown)
                    ? $"更弱，{delta:0.##}"
                    : $"更弱，{delta:0.##}（{breakdown}）";
            }

            return string.IsNullOrEmpty(breakdown) ? "相近" : $"相近（{breakdown}）";
        }

        public static string BuildSingleStatLine(string statKey, double value)
        {
            return statKey switch
            {
                "max_hp_flat" => $"HP+{(int)System.Math.Round(value)}",
                "attack_flat" => $"攻击+{(int)System.Math.Round(value)}",
                "defense_flat" => $"防御+{(int)System.Math.Round(value)}",
                "speed_flat" => $"速度+{(int)System.Math.Round(value)}",
                "crit_chance_delta" => $"暴击+{value:P0}",
                "crit_damage_delta" => $"暴伤+{value:0.##}",
                _ => $"{statKey}+{value:0.##}",
            };
        }

        public static string BuildModifierSummary(CharacterStatModifier modifier)
        {
            List<string> parts = new();
            if (modifier.MaxHpFlat != 0) parts.Add($"HP+{modifier.MaxHpFlat}");
            if (modifier.AttackFlat != 0) parts.Add($"攻击+{modifier.AttackFlat}");
            if (modifier.DefenseFlat != 0) parts.Add($"防御+{modifier.DefenseFlat}");
            if (modifier.SpeedFlat != 0) parts.Add($"速度+{modifier.SpeedFlat}");
            if (modifier.CritChanceDelta != 0.0) parts.Add($"暴击+{modifier.CritChanceDelta:P0}");
            if (modifier.CritDamageDelta != 0.0) parts.Add($"暴伤+{modifier.CritDamageDelta:0.##}");
            return parts.Count > 0 ? string.Join(" | ", parts) : "当前无额外词条";
        }

        private static double ScoreModifier(CharacterStatModifier modifier)
        {
            return modifier.MaxHpFlat * 0.08
                + modifier.AttackFlat * 1.0
                + modifier.DefenseFlat * 0.75
                + modifier.SpeedFlat * 0.6
                + modifier.MaxHpRate * 12.0
                + modifier.AttackRate * 12.0
                + modifier.DefenseRate * 10.0
                + modifier.SpeedRate * 8.0
                + modifier.CritChanceDelta * 120.0
                + modifier.CritDamageDelta * 20.0;
        }

        private static string BuildModifierDeltaBreakdown(CharacterStatModifier candidate, CharacterStatModifier current)
        {
            List<string> parts = new();
            AddDelta(parts, "攻击", candidate.AttackFlat - current.AttackFlat);
            AddDelta(parts, "防御", candidate.DefenseFlat - current.DefenseFlat);
            AddDelta(parts, "HP", candidate.MaxHpFlat - current.MaxHpFlat);
            AddDelta(parts, "速度", candidate.SpeedFlat - current.SpeedFlat);

            double critDelta = candidate.CritChanceDelta - current.CritChanceDelta;
            if (System.Math.Abs(critDelta) >= 0.0001)
            {
                parts.Add($"暴击{critDelta:+0%;-0%}");
            }

            double critDamageDelta = candidate.CritDamageDelta - current.CritDamageDelta;
            if (System.Math.Abs(critDamageDelta) >= 0.0001)
            {
                parts.Add($"暴伤{critDamageDelta:+0.##;-0.##}");
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" / ", parts);
        }

        private static void AddDelta(List<string> parts, string label, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            parts.Add($"{label}{delta:+#;-#}");
        }

        public static string BuildSlotLabel(EquipmentSlotType slot)
        {
            return UiText.SlotLabel(slot);
        }

        public static string GetSlotIcon(EquipmentSlotType slot)
        {
            return slot switch
            {
                EquipmentSlotType.Weapon => "⚔",
                EquipmentSlotType.Armor => "🛡",
                EquipmentSlotType.Accessory => "💎",
                _ => "●"
            };
        }

        public static string GetRarityColor(CharacterStatModifier modifier)
        {
            double score = ScoreModifier(modifier);
            if (score >= 40.0) return "#bf8c40";
            if (score >= 25.0) return "#8c66b3";
            if (score >= 12.0) return "#6689b3";
            return "#8c8072";
        }
    }
}
