namespace Xiuxian.Scripts.Services
{
    public readonly record struct MonsterStatProfile(
        string MonsterId,
        string DisplayName,
        CharacterStatBlock BaseStats,
        int InputsPerRound,
        string MoveCategory = "normal",
        bool IsBoss = false,
        string StrategyTag = "",
        int HealPerRound = 0)
    {
        public CharacterStatBlock ToStatBlock()
        {
            return BaseStats;
        }
    }
}
