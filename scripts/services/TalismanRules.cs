namespace Xiuxian.Scripts.Services
{
    public static class TalismanRules
    {
        public const double FireCharmAttackRate = 0.15;
        public const double ShieldCharmDefenseRate = 0.20;

        public static CharacterStatModifier GetModifier(string recipeId)
        {
            return recipeId switch
            {
                "talisman_fire_charm" => new CharacterStatModifier(AttackRate: FireCharmAttackRate),
                "talisman_shield_charm" => new CharacterStatModifier(DefenseRate: ShieldCharmDefenseRate),
                _ => default,
            };
        }
    }
}
