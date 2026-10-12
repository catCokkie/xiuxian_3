using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public static class OfflineSummaryPresentationRules
    {
        public static string BuildTitle(ActionSettlementResult result)
        {
            return result.ActionId switch
            {
                PlayerActionState.ActionCultivation => "离线修炼完成",
                PlayerActionState.ActionAlchemy => "离线炼丹完成",
                PlayerActionState.ActionSmithing => "离线炼器完成",
                PlayerActionState.ActionDungeon => "离线副本完成",
                PlayerActionState.ActionTalisman or PlayerActionState.ActionCooking or PlayerActionState.ActionFormation => "离线加工完成",
                _ => "离线基础修行完成",
            };
        }

        public static string BuildBody(ActionSettlementResult result)
        {
            List<string> labels = new();
            List<string> gains = new();

            if (result.ActionId == PlayerActionState.ActionDungeon)
            {
                labels.Add(result.BattleRoundsAdvanced > 0 ? "结果：副本有推进" : "结果：副本收益偏少");
            }
            else if (result.ActionId == PlayerActionState.ActionAlchemy)
            {
                labels.Add("结果：炼丹进度已结算");
            }
            else if (result.ActionId == PlayerActionState.ActionSmithing)
            {
                labels.Add("结果：炼器进度已结算");
            }
            else if (result.ActionId is PlayerActionState.ActionTalisman or PlayerActionState.ActionCooking or PlayerActionState.ActionFormation)
            {
                labels.Add("结果：加工进度已结算，基础修行持续");
            }
            else if (result.ActionId == PlayerActionState.ActionGarden)
            {
                labels.Add("结果：基础修行已结算，作物按实时时间生长并等待手动收获");
            }
            else
            {
                labels.Add(result.ActionId == PlayerActionState.ActionCultivation ? "结果：修炼收益已结算"
                    : "结果：基础修行已结算，当前采集或体修进度保留");
            }

            if (result.LingqiGain > 0.0)
            {
                gains.Add($"灵气+{result.LingqiGain:0}");
            }
            if (result.InsightGain > 0.0)
            {
                gains.Add($"悟性+{result.InsightGain:0}");
            }
            if (result.RealmExpGain > 0.0)
            {
                gains.Add($"境界经验+{result.RealmExpGain:0}");
            }
            if (result.ExploreProgressGain > 0.0)
            {
                gains.Add($"探索推进+{result.ExploreProgressGain:0.0}");
            }
            if (result.BattleRoundsAdvanced > 0)
            {
                gains.Add($"遭遇{result.BattleRoundsAdvanced}次");
            }
            foreach (KeyValuePair<string, int> drop in result.ItemDrops)
            {
                gains.Add($"{drop.Key}+{drop.Value}");
            }
            if (result.EquipmentDrops.Count > 0)
            {
                gains.Add($"装备+{result.EquipmentDrops.Count}");
            }

            if (gains.Count == 0)
            {
                return string.Join(" | ", labels) + " | 本次离线未获得可结算收益。";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < labels.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" | ");
                }
                sb.Append(labels[i]);
            }

            sb.Append("\n收益：");
            for (int i = 0; i < gains.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" | ");
                }
                sb.Append(gains[i]);
            }

            return sb.ToString();
        }
    }
}
